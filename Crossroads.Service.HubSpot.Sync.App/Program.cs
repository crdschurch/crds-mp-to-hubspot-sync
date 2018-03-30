using System;
using System.IO;
using System.Net.Http;
using AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Impl;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl;
using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Core.Time.Impl;
using Crossroads.Service.HubSpot.Sync.Core.Utilities.Impl;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Impl;
using Crossroads.Service.HubSpot.Sync.Data.MP;
using Crossroads.Service.HubSpot.Sync.Data.MP.Impl;
using Crossroads.Service.HubSpot.Sync.LiteDb.Configuration;
using Crossroads.Service.HubSpot.Sync.LiteDb.Configuration.Impl;
using Crossroads.Service.HubSpot.Sync.LiteDB;
using Crossroads.Service.HubSpot.Sync.LiteDB.Impl;
using Crossroads.Web.Common.Configuration;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JsonSerializer = Crossroads.Service.HubSpot.Sync.Core.Serialization.Impl.JsonSerializer;

namespace Crossroads.Service.HubSpot.Sync.App
{
    public class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        public static void Main(string[] args)
        {
            WireUpConfiguration();
            var serviceProvider = ConfigureServices();

            // load environment variable from .env for local development
            try
            {
                var newMpContactsToHubSpotJob = serviceProvider.GetService<ICreateNewMpRegistrationsInHubSpot>();

                newMpContactsToHubSpotJob.Execute();
            }
            catch (Exception e)
            {
                // no .env file present but since not required, just write
                Console.Write(e);
            }
        }

        public static void WireUpConfiguration()
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("./appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"./appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // define
            services.AddAutoMapper();

            CrossroadsWebCommonConfig.Register(services);

            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddLogging();
            services.AddSingleton<IClock, Clock>();
            services.AddSingleton<IJsonSerializer, JsonSerializer>();
            services.AddSingleton<IMinistryPlatformContactRepository, MinistryPlatformContactRepository>();
            services.AddSingleton<ICreateNewMpRegistrationsInHubSpot, CreateNewMpRegistrationsInHubSpot>();
            services.AddSingleton(new LiteDatabase("sync.db"));
            services.AddSingleton<ILiteDbRepository, LiteDbRepositoryWrapper>();
            services.AddSingleton<ILiteDbConfigurationProvider, LiteDbConfigurationProvider>();
            services.AddSingleton<IJobRepository, JobRepository>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton(Configuration);

            services.AddSingleton<ICreateOrUpdateContactsInHubSpot>(context =>
                new CreateOrUpdateContactsInHubSpot(
                    new HttpPost(
                        new HttpClient { BaseAddress = new Uri(Configuration["HubSpotApiBaseUrl"]) },
                        context.GetService<IJsonSerializer>(),
                        context.GetService<ILogger<HttpPost>>()),
                    Environment.GetEnvironmentVariable("HUBSPOT_API_KEY"),
                    context.GetService<ILogger<CreateOrUpdateContactsInHubSpot>>()));

            // construct
            var serviceProvider = services.BuildServiceProvider();

            // consume
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            // Hackery for setting the application's base path for impending log files
            log4net.GlobalContext.Properties["AppLogRoot"] = Environment.GetEnvironmentVariable("APP_LOG_ROOT");
            loggerFactory.AddLog4Net("log4net.config"); // defaults to this in root -- being explicit for the sake of explicit transparency/clarity
            loggerFactory
                .AddConsole()
                .AddDebug();

            return serviceProvider;
        }
    }
}