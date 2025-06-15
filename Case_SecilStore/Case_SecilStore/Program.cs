using SecilStore_ConfigLibrary.Extensions;
using SecilStore_ConfigLibrary.Options;
using SecilStore_ConfigLibrary.Repositories;
using Microsoft.Extensions.Logging;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Configuration/Index", "");
});
builder.Services.AddControllers();

// Loglama seviyesini artır
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

Console.WriteLine("Starting application...");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

// MongoDB bağlantı bilgisi
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017";
Console.WriteLine($"MongoDB connection string: {mongoConnectionString}");

// Docker ortamında MongoDB'nin başlamasını bekleyin
if (builder.Environment.IsProduction() || Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Docker")
{
    Console.WriteLine("Waiting for MongoDB to start...");
    System.Threading.Thread.Sleep(10000); // 10 saniye bekle
}

try
{
    // Konfigürasyon servislerini ekle
    builder.Services.AddConfigurationServices(new ConfigurationOptions
    {
        ApplicationName = "SERVICE-A", // Varsayılan uygulama adı
        ConnectionString = mongoConnectionString,
        RefreshTimerIntervalInMs = 60000, // 1 dakika
        StorageType = StorageType.MongoDB,
        DatabaseName = "ConfigurationDb",
        CollectionName = "Configurations"
    });
}
catch (Exception ex)
{
    Console.WriteLine($"Error adding configuration services: {ex.Message}");
    // Hata durumunda uygulama başlangıcını engellemiyoruz
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// HTTPS yönlendirmesini kaldır
// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers(); // API Controller'lar için endpoint'leri ekle

Console.WriteLine("Application started. Endpoints mapped.");

app.Run();
