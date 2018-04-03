using Arts.Entity.Data;
using Arts.Entity.Models;
using Arts.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc();

            var auth = services.AddAuthentication();

            auth.AddFacebook(facebookOptions =>
            {
                facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
                facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
            });

            auth.AddGoogle(options =>
            {
                options.ClientId = Configuration["Authentication:Google:ClientId"];
                options.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
            });

            //auth.AddMicrosoftAccount(options =>
            //{
            //    options.ClientId = Configuration["Authentication:Microsoft:ClientId"];
            //    options.ClientSecret = Configuration["Authentication:Microsoft:ClientSecret"];
            //});

            auth.AddTwitter(options =>
            {
                options.ConsumerKey = Configuration["Authentication:Twitter:ConsumerKey"];
                options.ConsumerSecret = Configuration["Authentication:Twitter:ConsumerSecret"];
            });
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
    }
}
