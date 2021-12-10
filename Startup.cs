/*
Copyright 2021, E.J. Wilburn, Marcus McKinnon, Kevin Williams
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

using AutoMapperBuilder.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NuGet.Protocol.Core.Types;
using PalaverCore.Data;
using PalaverCore.Models;
using PalaverCore.Models.MappingProfiles;
using PalaverCore.Services;
using PalaverCore.SignalR;
using System.Text.Json.Serialization;

namespace PalaverCore;

public class Startup
{
    public static string SiteRoot = "";

    public Startup(IWebHostEnvironment env)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

        builder.AddEnvironmentVariables();
        Configuration = builder.Build();
        SiteRoot = Configuration["SiteRoot"].ToString();
        if (!SiteRoot.EndsWith("/"))
            SiteRoot += "/";
    }

    public IConfigurationRoot Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews();
        services.AddRazorPages();

        var commentRenderer = new CommentRenderService();
        services.AddSingleton<CommentRenderService>(commentRenderer);
        services.AddAutoMapperBuilder(builder =>
        {
            builder.Profiles.Add(new CommentMappingProfile(commentRenderer));
            builder.Profiles.Add(new ThreadMappingProfile());
        });

        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                // Keep properties PascalCase so the same renderer can be used server and client side.
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
            });

        // Add framework services.
        services.AddDbContext<PalaverDbContext>(options =>
            options.UseNpgsql(Configuration.GetConnectionString("Palaver")));
        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddIdentity<User, Role>()
            .AddEntityFrameworkStores<PalaverDbContext>()
            .AddDefaultTokenProviders();

        // Add forced identity options.
        services.ConfigureApplicationCookie(options => {
            options.LoginPath = "/Account/LogIn";
            options.LogoutPath = "/Account/LogOff";
            options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
        });

        var googleConfig = Configuration.GetSection("GoogleOptions");
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddGoogle(options => {
                    options.ClientId = googleConfig["ClientId"];
                    options.ClientSecret = googleConfig["ClientSecret"];
                    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
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
        services.AddHealthChecks();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
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

        app.UseWebSockets();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseCookiePolicy(new CookiePolicyOptions
        {
            MinimumSameSitePolicy = SameSiteMode.Lax
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHub<SignalrHub>("/threads");
            endpoints.MapControllerRoute("default", "{controller=Thread}/{action=Index}/{id?}");
            endpoints.MapHealthChecks("/health");
            endpoints.MapRazorPages();
        });
    }
}