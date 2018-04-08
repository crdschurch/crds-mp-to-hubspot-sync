using System;
using System.Net.Http;
using AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Dto;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Impl;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl;
using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Core.Time.Impl;
using Crossroads.Service.HubSpot.Sync.Core.Utilities;
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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JsonSerializer = Crossroads.Service.HubSpot.Sync.Core.Serialization.Impl.JsonSerializer;

namespace Crossroads.Service.HubSpot.Sync.App
{
    public class Startup
    {
        public Startup(IHostingEnvironment hostingEnvironment)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(hostingEnvironment.ContentRootPath)
                .AddJsonFile("./appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"./appsettings.{hostingEnvironment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddRouting(options => options.LowercaseUrls = false);
            services.AddCors();

            CrossroadsWebCommonConfig.Register(services); // temp solution for debugging

            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddLogging();
            services.AddSingleton<IClock, Clock>();
            services.AddSingleton<ISleep, Sleeper>();
            services.AddSingleton<IJsonSerializer, JsonSerializer>();
            services.AddSingleton<IMinistryPlatformContactRepository, MinistryPlatformContactRepository>();
            services.AddSingleton<ISyncNewMpRegistrationsToHubSpot, SyncNewMpRegistrationsToHubSpot>();
            services.AddSingleton(new LiteDatabase("filename=sync.db;utc=true"));
            services.AddSingleton<ILiteDbRepository, LiteDbRepositoryWrapper>();
            services.AddSingleton<ILiteDbConfigurationProvider, LiteDbConfigurationProvider>();
            services.AddSingleton<IJobRepository, JobRepository>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton(Configuration);
            services.Configure<InauguralSync>(Configuration.GetSection("InauguralSync"));

            services.AddSingleton<ICreateOrUpdateContactsInHubSpot>(context =>
                new CreateOrUpdateContactsInHubSpot(
                    new HttpJsonPost(
                        new HttpClient { BaseAddress = new Uri(Configuration["HubSpotApiBaseUrl"]) },
                        context.GetService<IJsonSerializer>(),
                        context.GetService<ILogger<HttpJsonPost>>()),
                    context.GetService<IClock>(),
                    context.GetService<IJsonSerializer>(),
                    context.GetService<ISleep>(),
                    Configuration["HUBSPOT_API_KEY"], // env variable
                    context.GetService<ILogger<CreateOrUpdateContactsInHubSpot>>()));

            services.AddSingleton(provider => new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile(provider.GetService<IConfigurationService>()));
            }).CreateMapper());

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // Hackery for setting the application's base path for impending log files
            log4net.GlobalContext.Properties["AppLogRoot"] = Environment.GetEnvironmentVariable("APP_LOG_ROOT");
            loggerFactory.AddLog4Net("log4net.config"); // defaults to this in root -- being explicit for the sake of explicit transparency/clarity

            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
            app.UseMvc();
        }
    }
}
