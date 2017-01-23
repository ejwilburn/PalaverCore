using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;

namespace Palaver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
//                .SetBasePath(Directory.GetCurrentDirectory())
//                .AddJsonFile("appsettings.json", false)
                .AddCommandLine(args)
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();
           
            var host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseKestrel(options => {
                    // options.Listen(IPAddress.Any, 5001, listenOptions => {
                    //     listenOptions.UseHttps("testCert.pfx", "testPassword");
                    // });
                    options.UseHttps("testCert.pfx", "testPassword");
                })
                .UseUrls("http://*:5000/", "https://*:5001")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseSetting("detailedErrors", "true")
                .CaptureStartupErrors(true)
                .Build();

            host.Run();
        }
    }
}
