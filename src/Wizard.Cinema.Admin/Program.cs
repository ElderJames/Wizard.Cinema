﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Wizard.Cinema.Admin
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)

                .UseStartup<Startup>();
    }
}
