//using IServiceCollection.interfaces;
using MusicService.interfaces;
using myMusic.Services;
using MyMiddleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMusicService();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Logging.ClearProviders();//log4net seriLog//try
builder.Logging.AddConsole(); //try
builder.Services.AddOpenApi();



var app = builder.Build();


 app.UseMyLogMiddleware();//try

//app.Run(async context => await context.Response.WriteAsync("our no-map terminal 2nd middleware!\n"));//Try


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
   
    app.MapOpenApi();

        app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
 
}

//הוספה עכשיו
app.UseDefaultFiles();
app.UseStaticFiles();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
