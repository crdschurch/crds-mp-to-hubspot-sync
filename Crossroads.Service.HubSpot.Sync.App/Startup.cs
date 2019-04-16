using AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Dto;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration.Impl;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Validation;
using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Core.Time.Impl;
using Crossroads.Service.HubSpot.Sync.Core.Utilities;
using Crossroads.Service.HubSpot.Sync.Core.Utilities.Guid;
using Crossroads.Service.HubSpot.Sync.Core.Utilities.Guid.Impl;
using Crossroads.Service.HubSpot.Sync.Core.Utilities.Impl;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Impl;
using Crossroads.Service.HubSpot.Sync.Data.MP;
using Crossroads.Service.HubSpot.Sync.Data.MP.Impl;
using Crossroads.Web.Common.Configuration;
using DalSoft.Hosting.BackgroundQueue.DependencyInjection;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Net.Http;
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
            services.AddMvc().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<ActivityValidator>()); ;
            services.AddRouting(options => options.LowercaseUrls = false);
            services.AddCors();

            CrossroadsWebCommonConfig.Register(services);

            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddLogging();
            services.AddSingleton<IClock, Clock>();
            services.AddSingleton<IGenerateCombGuid, GenerateCombGuid>();
            services.AddSingleton<ISleep, Sleeper>();
            services.AddSingleton<IJsonSerializer, JsonSerializer>();
            services.AddSingleton<IMinistryPlatformContactRepository, MinistryPlatformContactRepository>();
            services.AddSingleton<ISyncMpContactsToHubSpotService, SyncMpContactsToHubSpotService>();
            services.AddSingleton<IPrepareMpDataForHubSpot, PrepareMpDataForHubSpot>();
            services.AddSingleton(sp => new MongoClient(Configuration["MONGO_DB_CONN"]).GetDatabase("hubspotsync")); // Mongo stores UTC by default
            services.AddSingleton<IJobRepository, JobRepository>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<ICleanUpActivity, CleanUpActivity>();
            services.AddSingleton(Configuration);
            services.Configure<InauguralSync>(Configuration.GetSection("InauguralSync"));
            services.Configure<DocumentDbSettings>(Configuration.GetSection("DocumentDbSettings"));

            services.AddSingleton<ICreateOrUpdateContactsInHubSpot>(context =>
                new CreateOrUpdateContactsInHubSpot(
                    new HttpClientFacade(
                        new HttpClient { BaseAddress = new Uri(Configuration["HubSpotApiBaseUrl"]) },
                        context.GetService<IJsonSerializer>(),
                        context.GetService<ILogger<HttpClientFacade>>()),
                    context.GetService<IClock>(),
                    context.GetService<IJsonSerializer>(),
                    context.GetService<ISleep>(),
                    Configuration["TEST_HUBSPOT_API_KEY"] ?? Configuration["HUBSPOT_API_KEY"], // env variable
                    context.GetService<ILogger<CreateOrUpdateContactsInHubSpot>>()));

            services.AddSingleton(provider => new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile(provider.GetService<IConfigurationService>()));
            }).CreateMapper());

            services.AddBackgroundQueue(maxConcurrentCount: 1, millisecondsToWaitBeforePickingUpTask: 1000,
                onException: exception =>
                    throw new Exception("An exception occurred while a background process was executing.", exception));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // Hackery for setting the application's base path for impending log files
            log4net.GlobalContext.Properties["AppLogRoot"] = Configuration["APP_LOG_ROOT"];
            log4net.GlobalContext.Properties["environment"] = Configuration["CRDS_ENV"];
            loggerFactory.AddLog4Net("log4net.config"); // defaults to this in root -- being explicit for the sake of transparency/clarity

            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
            app.UseMvc();
        }
    }
}
