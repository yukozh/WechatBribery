using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WechatBribery.Models;

namespace WechatBribery
{
    public class Startup
    {
        public static IConfiguration Config;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSignalR();
            services.AddLogging();
            services.AddConfiguration(out Config);
            services.AddSmartCookies();
            services.AddSmartUser<User, string>();
            services.AddMemoryCache();
            services.AddSession(o =>
            {
                o.IdleTimeout = new System.TimeSpan(0, 20, 0);
            });

            services.AddDbContext<BriberyContext>(x => x.UseMySql("Server=localhost;database=wechat;uid=root;pwd=Sun060810;charset=utf8"));

            services.AddIdentity<User, IdentityRole>(x =>
            {
                x.Password.RequireDigit = false;
                x.Password.RequiredLength = 0;
                x.Password.RequireLowercase = false;
                x.Password.RequireNonAlphanumeric = false;
                x.Password.RequireUppercase = false;
                x.User.AllowedUserNameCharacters = null;
            })
                .AddDefaultTokenProviders()
                .AddEntityFrameworkStores<BriberyContext>();
        }

        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Error, true);
            app.UseSession();
            app.UseSignalR();
            app.UseIdentity();
            app.UseDeveloperExceptionPage();
            app.UseBlobStorage();
            app.UseMvcWithDefaultRoute();
            app.UseStaticFiles();

            await SampleData.InitDB(app.ApplicationServices);
        }
    }
}
