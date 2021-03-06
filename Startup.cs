/*
Copyright 2017, E.J. Wilburn, Marcus McKinnon, Kevin Williams
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

using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PalaverCore.Data;
using PalaverCore.Models;
using PalaverCore.Services;
using PalaverCore.SignalR;

namespace PalaverCore
{
	public class Startup
    {
        public static string SiteRoot = "";

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
            SiteRoot = Configuration["SiteRoot"];
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddAutoMapper();
            services.AddSignalR();

            // Add framework services.
            services.AddDbContext<PalaverDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("Palaver")));

            services.AddIdentity<User, Role>()
                .AddEntityFrameworkStores<PalaverDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
            // Add forced identity options.
            services.ConfigureApplicationCookie(options => {
                options.LoginPath = "/Account/LogIn";
                options.LogoutPath = "/Account/LogOff";
                options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
            });

            // services.AddAuthentication()
            //         .AddGoogle();
            var googleConfig = Configuration.GetSection("GoogleOptions");
            services.AddAuthentication()
                    .AddGoogle(options => {
                        options.ClientId = googleConfig["ClientId"];
                        options.ClientSecret = googleConfig["ClientSecret"];
                    });
            
            services.Configure<IdentityOptions>(options =>
            {
                // User settings
                options.User.RequireUniqueEmail = true;
            });
            services.Configure<IdentityOptions>(Configuration.GetSection("IdentityOptions"));

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddSingleton<StubbleRendererService>(new StubbleRendererService(Configuration.GetValue<bool>("CacheTemplates")));
            services.Configure<SmtpOptions>(Configuration.GetSection(SmtpOptions.CONFIG_SECTION_NAME));
            services.Configure<GoogleOptions>(Configuration.GetSection("GoogleOptions"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            if (env.IsDevelopment())
            {
    			loggerFactory.WithFilter(new FilterLoggerSettings{
                        { "Default", LogLevel.Warning },
                        { "Microsoft.EntityFrameworkCore", LogLevel.Information }
                    }).AddDebug();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            app.UseHttpMethodOverride();

            // Use a non-root base path for the external URL for the site if configured.
            if (!string.IsNullOrEmpty(SiteRoot))
                app.UsePathBase(SiteRoot);

            // Set up custom content types -associating file extension to MIME type
            // This has to be done for supporting the .mustache template files, either
            // that or allow all unknown types.
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".mustache"] = "text/html";

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = provider
            });

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Thread}/{action=Index}/{id?}");
            });

            app.UseWebSockets();
            app.UseSignalR( routes => {
                routes.MapHub<SignalrHub>("threads");
            });
        }
    }
}
