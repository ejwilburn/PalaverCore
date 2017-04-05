/*
Copyright 2017, Marcus McKinnon, E.J. Wilburn, Kevin Williams
This program is distributed under the terms of the GNU General Public License.

This file is part of Palaver.

Palaver is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

Palaver is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Palaver.  If not, see <http://www.gnu.org/licenses/>.
*/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Palaver.Data;
using Palaver.Models;
using Palaver.Services;
using Microsoft.AspNetCore.StaticFiles;

namespace Palaver
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddAutoMapper();

            services.AddSignalR(options => options.Hubs.EnableDetailedErrors = true);

            // Add framework services.
            services.AddDbContext<PalaverDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("Palaver")));

            services.AddIdentity<User, Role>()
                .AddEntityFrameworkStores<PalaverDbContext, int>()
                .AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();

            // Add primary config-file based options.
            services.Configure<IdentityOptions>(Configuration.GetSection("IdentityOptions"));
            // Add forced identity options.
            services.Configure<IdentityOptions>(options =>
            {
                // Cookie settings.
                options.Cookies.ApplicationCookie.LoginPath = "/Account/LogIn";
                options.Cookies.ApplicationCookie.LogoutPath = "/Account/LogOff";
                options.Cookies.ApplicationCookie.AutomaticAuthenticate = true;
                options.Cookies.ApplicationCookie.AuthenticationScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
                options.Cookies.ApplicationCookie.ReturnUrlParameter = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.ReturnUrlParameter;

                // User settings
                options.User.RequireUniqueEmail = true;
            });

            services.Configure<SmtpOptions>(Configuration.GetSection(SmtpOptions.CONFIG_SECTION_NAME));
            services.Configure<GoogleOptions>(Configuration.GetSection("GoogleOptions"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            if (env.IsDevelopment())
            {
                loggerFactory.AddDebug();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Set up custom content types -associating file extension to MIME type
            // This has to be done for supporting the .mustache template files, either
            // that or allow all unknown types.
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".mustache"] = "text/html";

            app.UseStaticFiles(new StaticFileOptions {
                ContentTypeProvider = provider
            });

            app.UseIdentity();

            app.UseGoogleAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Thread}/{action=Index}/{id?}");
            });

            app.UseWebSockets();
            app.UseSignalR();
        }
    }
}
