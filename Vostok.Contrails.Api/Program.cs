﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Vostok.Instrumentation.AspNetCore;

namespace Vostok.Contrails.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        private static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", false, true);
                })
                .UseUrls("http://+:6302/")
                .ConfigureAirlock()
                .ConfigureVostokMetrics()
                .ConfigureVostokLogging()
                .UseStartup<Startup>()
                .Build();
        }
    }
}
