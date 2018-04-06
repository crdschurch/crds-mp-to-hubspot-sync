using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Crossroads.Service.HubSpot.Sync.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // load environment variable from .env for local development
            try
            {
                DotNetEnv.Env.Load("../.env");
            }
            catch (Exception e)
            {
                // no .env file present but since not required, just write
                Console.Write(e);
            }

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}