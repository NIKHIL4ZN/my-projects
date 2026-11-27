using Microsoft.EntityFrameworkCore;
using SmartX.Core.Interfaces;
using SmartX.Core.Validation;
using SmartX.Infrastructure;
using SmartX.Infrastructure.Data;
using SmartX.Infrastructure.Services;
using System.Text.Json.Serialization;
using SmartX.Core.Commands.Interfaces;
using SmartX.Infrastructure.Commands;
using SmartX.Infrastructure.Topology;
using SmartX.Core.Topology.Interfaces;

namespace SmartX.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder =
                WebApplication.CreateBuilder(args);

            var connectionString =
                builder.Configuration.GetConnectionString(
                    "SmartXDatabase")
                ?? throw new InvalidOperationException(
                    "Connection string 'SmartXDatabase' " +
                    "was not found.");

            var storagePath =
                builder.Configuration[
                    "FileStorage:RootPath"]
                ?? throw new InvalidOperationException(
                    "File storage root path was not " +
                    "configured.");

            var maximumFileSizeBytes =
                builder.Configuration.GetValue<long>(
                    "FileStorage:MaximumFileSizeBytes");

            var encryptionKey =
                builder.Configuration[
                    "FileEncryption:Key"]
                ?? throw new InvalidOperationException(
                    "The file encryption key was not " +
                    "configured.");

            var absoluteStoragePath =
                Path.Combine(
                    builder.Environment.ContentRootPath,
                    storagePath);

            builder.Services.AddDbContext<SmartXDbContext>(
                options =>
                    options.UseSqlServer(connectionString));

            builder.Services.AddScoped<
                ISensorService,
                SensorService>();

            builder.Services.AddSingleton<TelemetryHistoryIndex>();

            builder.Services.AddScoped<
                ITelemetryService,
                TelemetryService>();

            builder.Services.AddSingleton<
                IDeploymentTreeValidator,
                DeploymentTreeValidator>();

            builder.Services.AddScoped<
                ISensorAttachmentService>(
                serviceProvider =>
                    new EncryptedSensorAttachmentService(
                        serviceProvider.GetRequiredService<
                            SmartXDbContext>(),
                        absoluteStoragePath,
                        maximumFileSizeBytes,
                        encryptionKey));

            builder.Services.AddScoped<
    IAnomalyInvestigationService,
    AnomalyInvestigationService>();

            builder.Services.AddSingleton<
        ICommandStreamService,
        CommandStreamService>();

            builder.Services.AddSingleton<
            IOperatorQueryTracker,
            OperatorQueryTracker>();

            builder.Services.AddScoped<
    IPredictiveActionService,
    PredictiveActionService>();

            builder.Services.AddScoped<
            ITopologyService,
            TopologyService>();

            //Service reg
            builder.Services
     .AddControllers()
     .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.Converters.Add(
             new JsonStringEnumConverter());
     });

            builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "SmartXClient",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173",
                    "http://localhost:5174",
                    "http://127.0.0.1:5173",
                    "http://127.0.0.1:5174")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});
            builder.Services.AddAuthorization();
            builder.Services.AddOpenApi();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                await using var scope =
                    app.Services.CreateAsyncScope();

                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<
                            SmartXDbContext>();

                await SmartXDataSeeder.SeedAsync(
    dbContext);

                await SmartXTopologySeeder.SeedAsync(
                    dbContext);

                app.MapOpenApi();
            }

            // Middleware
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode =
                        StatusCodes.Status500InternalServerError;

                    await context.Response.WriteAsJsonAsync(new
                    {
                        message =
                            "An unexpected server error occurred. " +
                            "Please try again.",
                        traceId = context.TraceIdentifier
                    });
                });
            });

            app.UseCors("SmartXClient");

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}

// Code attributes

/*
Title: Handle errors in ASP.NET Core
Author(s): Microsoft
Date: 2026
Version: ASP.NET Core 10.0
Type: Documentation
Availability: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0
*/

/*
Title: Serialization and Deserialization in C#
Author(s): Fahad Naeem
Date: 2023
Version: Not applicable
Type: Online article
Availability: https://www.c-sharpcorner.com/article/serialization-and-deserialization-in-c-sharp
*/
