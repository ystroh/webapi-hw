using MusicService.interfaces;
using myMusic.Services;
using myMusic.Models;
using MyMiddleware;
using myUsers.Services;
using myUsers.Models;
using Common.Repositories;
using Common.Services;
using Common.Active;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using TokenServices.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Models;
// using Serilog; // הושם בהערה

var builder = WebApplication.CreateBuilder(args);

// הושם בהערה - Serilog configuration
/*
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("Logs/music_shop_log.txt", 
        fileSizeLimitBytes: 52428800, 
        rollOnFileSizeLimit: true, 
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(); 
*/
builder.Services.AddUsersService();

// Add services to the container.
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values.SelectMany(v => v.Errors);
        Console.WriteLine($"DEBUG: Model Binding Error - {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
        return new BadRequestObjectResult(context.ModelState);
    };
});
builder.Services.AddMusicService();


// רישום ריפוזיטוריות JSON
builder.Services.AddSingleton<IRepository<myUsers.Models.Users>>(sp =>
    new JsonFileRepository<myUsers.Models.Users>(sp.GetRequiredService<IWebHostEnvironment>(), System.IO.Path.Combine("Data", "users.json")));

builder.Services.AddSingleton<IRepository<myMusic.Models.Music>>(sp =>
    new JsonFileRepository<myMusic.Models.Music>(sp.GetRequiredService<IWebHostEnvironment>(), System.IO.Path.Combine("Data", "music.json")));

builder.Services.AddSingleton<LogQueue>();
builder.Services.AddHostedService<BackgroundWorker>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActiveUser, ActiveUser>();

// הגדרות Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie() 
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = TokenService.GetTokenValidationParameters();
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"].FirstOrDefault();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    })
    .AddGoogle(options =>
{
    options.ClientId = "862846808441-aobmot69hr735vr7r8jinqbcrshb9fdo.apps.googleusercontent.com";
    options.ClientSecret = "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"; 
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// החזרת הלוגר הרגיל של ה-Console
builder.Logging.ClearProviders();
builder.Logging.AddConsole(); 
builder.Logging.AddDebug();

builder.Services.AddOpenApi();
builder.Services.AddSignalR();

var app = builder.Build();

// סדר ה-Middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseDefaultFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMyLogMiddleware();

app.MapControllers();
app.MapHub<MusicHub>("/musicHub");

app.Run();