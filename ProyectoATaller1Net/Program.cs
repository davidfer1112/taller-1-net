using System.Text;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient for ProyectoB
builder.Services.AddHttpClient("ProyectoBClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5281/"); 
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Map GET endpoint for weather forecast
app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapPost("/sendToB", async (IHttpClientFactory clientFactory, HttpRequest request) =>
{
    var client = clientFactory.CreateClient("ProyectoBClient");
    string requestBody;
    using (var reader = new StreamReader(request.Body))
    {
        requestBody = await reader.ReadToEndAsync();
    }
    var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

    try
    {
        var response = await client.PostAsync("api/data", content);
        return response.IsSuccessStatusCode ? Results.Ok(await response.Content.ReadAsStringAsync()) : Results.Problem("Problema al contactar a Proyecto B");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("SendToB")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
