using Arts.Entity.Data;
using Arts.Entity.Models;
using Arts.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Arts.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                var builder = new ConfigurationBuilder();
                builder.AddUserSecrets<Startup>();
                Configuration = builder.Build();
            }
            else
            {
                Configuration = configuration;
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                var connectionString = Configuration["DefaultConnection"];
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new Exception("The default database connection string is not configured. Set up " +
                        "a user secret or application configuration file for this application to run properly. " +
                        "For details on how to do that, follow this link "  +
                        "https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?tabs=visual-studio " +
                        "or run the following from a command prompt:\n" +
                        "> dotnet user-secrets set DefaultConnection <your database connection string>");
                }
                options.UseSqlServer(connectionString);
            });

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc();

            ConfigureAuthentication(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "areas",
                    template: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
                );

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        #region authentication helpers

        public void ConfigureAuthentication(IServiceCollection services)
        {
            var auth = services.AddAuthentication();

            AddAuth("Facebook", (id, secret) => auth.AddFacebook(options =>
            {
                options.AppId = id;
                options.AppSecret = secret;
            }));

            AddAuth("Google", (id, secret) => auth.AddGoogle(options =>
            {
                options.ClientId = id;
                options.ClientSecret = secret;
            }));

            AddAuth("Microsoft", (id, secret) => auth.AddMicrosoftAccount(options =>
            {
                options.ClientId = id;
                options.ClientSecret = secret;
            }));

            AddAuth("Twitter", (id, secret) => auth.AddTwitter(options =>
            {
                options.ConsumerKey = id;
                options.ConsumerSecret = secret;
            }));
        }

        private bool HasAuthentication(string providerName, out string appId, out string appSecret)
        {
            appId = Configuration[$"Authentication:{providerName}:ClientId"];
            appSecret = Configuration[$"Authentication:{providerName}:ClientSecret"];

            return !string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(appSecret);
        }

        private void AddAuth(string providerName, Action<string, string> onSuccess)
        {
            if (HasAuthentication(providerName, out var id, out var secret))
            {
                onSuccess(id, secret);
            }
        }

        #endregion
    }
}
