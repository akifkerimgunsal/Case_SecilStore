using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecilStore_ConfigLibrary;
using SecilStore_ConfigLibrary.Options;
using SecilStore_ConfigWebUI.Services;
using SecilStore_ConfigLibrary.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace SecilStore_ConfigWebUI
{
    // DTO for configuration display
    public class ConfigurationDisplayDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Value { get; set; }
        public bool IsActive { get; set; }
        public string? ApplicationName { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddControllers();

            // CORS politikasını ekle
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // MongoDB bağlantı bilgisi
            var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
            var databaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "ConfigurationDb";
            var collectionName = builder.Configuration["MongoDB:CollectionName"] ?? "Configurations";

            // RabbitMQ bağlantı bilgisi
            var rabbitMQConnectionString = builder.Configuration["RabbitMQ:ConnectionString"] ?? "amqp://guest:guest@localhost:5672";
            var useRabbitMQ = builder.Configuration.GetValue<bool>("RabbitMQ:UseRabbitMQ", false);

            // Konfigürasyon seçenekleri
            var configOptions = new ConfigurationOptions
            {
                ApplicationName = "SecilStore",
                ConnectionString = mongoConnectionString,
                RefreshTimerIntervalInMs = 60000, // 1 dakika
                DatabaseName = databaseName,
                CollectionName = collectionName,
                RabbitMQConnectionString = rabbitMQConnectionString,
                UseRabbitMQ = useRabbitMQ
            };

            // Konfigürasyon servislerini ekle
            builder.Services.AddConfigurationServices(configOptions);

            // Uygulama servislerini ekle
            builder.Services.AddScoped<ConfigurationService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseCors("AllowAll");
            app.UseHttpsRedirection();

            // Ensure wwwroot directory exists
            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (!Directory.Exists(wwwrootPath))
            {
                Directory.CreateDirectory(wwwrootPath);
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllers();

            // API endpoint'leri
            app.MapGet("/api/configurations", async (ConfigurationService configService) =>
            {
                var configs = await configService.GetAllConfigurationsAsync();
                var displayConfigs = configs.Select(c => new ConfigurationDisplayDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Type = c.Type ?? DetermineType(c.Value),
                    Value = c.Value,
                    IsActive = c.IsActive,
                    ApplicationName = c.ApplicationName
                }).ToList();
                
                return Results.Ok(displayConfigs);
            });

            app.MapGet("/api/configurations/{key}", async (string key, ConfigurationService configService) =>
            {
                try
                {
                    var value = await configService.GetValueAsync<string>(key);
                    return Results.Ok(value);
                }
                catch (Exception ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
            });

            app.MapPost("/api/configurations", async (ConfigurationEntry config, ConfigurationService configService) =>
            {
                var result = await configService.AddConfigurationAsync(config);
                return Results.Created($"/api/configurations/{config.Name}", result);
            });

            app.MapPut("/api/configurations", async (ConfigurationEntry config, ConfigurationService configService) =>
            {
                var result = await configService.UpdateConfigurationAsync(config);
                return Results.Ok(result);
            });

            app.MapDelete("/api/configurations/{id}", async (string id, ConfigurationService configService) =>
            {
                var result = await configService.DeleteConfigurationAsync(id);
                return result ? Results.NoContent() : Results.NotFound();
            });

            // Basit bir test endpoint'i
            app.MapGet("/api/test", () => Results.Ok(new { message = "API çalışıyor!" }));

            // Redirect root to index.html
            app.MapGet("/", () => Results.Redirect("/index.html"));

            app.Run();
        }

        // Helper method to determine type from value
        private static string DetermineType(string value)
        {
            if (bool.TryParse(value, out _))
                return "bool";
            
            if (int.TryParse(value, out _))
                return "int";
            
            if (double.TryParse(value, out _))
                return "double";
            
            return "string";
        }
    }
} 