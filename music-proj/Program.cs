using MusicService.interfaces;
using myMusic.Services;
using myMusic.Models;
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

var builder = WebApplication.CreateBuilder(args);

// --- רישום שירותי מערכת (Dependency Injection) ---

// רישום שירותי המשתמשים והמוזיקה באמצעות Extension Methods
builder.Services.AddUsersService();
builder.Services.AddMusicService();

// הגדרת בקרים וטיפול בשגיאות Model Validation
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values.SelectMany(v => v.Errors);
        Console.WriteLine($"DEBUG: Model Binding Error - {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
        return new BadRequestObjectResult(context.ModelState);
    };
});

// רישום ריפוזיטוריות גנריות לניהול קבצי JSON (Singleton)
builder.Services.AddSingleton<IRepository<myUsers.Models.Users>>(sp =>
    new JsonFileRepository<myUsers.Models.Users>(sp.GetRequiredService<IWebHostEnvironment>(), Path.Combine("Data", "users.json")));

builder.Services.AddSingleton<IRepository<myMusic.Models.Music>>(sp =>
    new JsonFileRepository<myMusic.Models.Music>(sp.GetRequiredService<IWebHostEnvironment>(), Path.Combine("Data", "music.json")));

// רישום מערכת הלוגים האסינכרונית (תור ושירות רקע)
builder.Services.AddSingleton<LogQueue>();
builder.Services.AddHostedService<BackgroundWorker>();

// הגדרת גישה ל-HttpContext ולמשתמש המחובר (Active User)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActiveUser, ActiveUser>();

// --- הגדרות אימות והרשאות (Authentication & Authorization) ---

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
                // תמיכה בשליפת טוקן מה-Query String עבור SignalR
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
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    });

// הגדרת מדיניות הרשאות
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// --- הגדרות תשתית נוספות ---

builder.Logging.ClearProviders();
builder.Logging.AddConsole(); 

builder.Services.AddOpenApi();
builder.Services.AddSignalR();

var app = builder.Build();

// --- הגדרת צינור הבקשות (Middleware Pipeline) ---

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

// סדר קריטי: אימות לפני הרשאות
app.UseAuthentication();
app.UseAuthorization();

// Middleware מותאם אישית לרישום לוגים של בקשות
app.UseMiddleware<RequestLoggingMiddleware>();

// ניתוב סופי לקונטרולרים ול-SignalR Hub
app.MapControllers();
app.MapHub<MusicHub>("/musicHub");

app.Run();