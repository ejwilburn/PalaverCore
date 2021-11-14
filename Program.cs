using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PalaverCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddCommandLine(args)
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseConfiguration(config)
                        .ConfigureLogging((hostingContext, logging) => {
                            logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                            logging.AddConsole();
                            if (hostingContext.HostingEnvironment.IsDevelopment())
                            {
                                logging.AddDebug();
                            }
                        })
                        .UseKestrel()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseStartup<Startup>()
                        .UseSetting("detailedErrors", "true")
                        .CaptureStartupErrors(true);
                });
        }
    }
}
