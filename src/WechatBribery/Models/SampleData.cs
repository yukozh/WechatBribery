using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WechatBribery.Models
{
    public static class SampleData
    {
        public static async Task InitDB(IServiceProvider services)
        {
            var DB = services.GetRequiredService<BriberyContext>();
            var UserManager = services.GetRequiredService<UserManager<User>>();
            var RoleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            DB.Database.EnsureCreated();
            await RoleManager.CreateAsync(new IdentityRole("Root"));
            await RoleManager.CreateAsync(new IdentityRole("Member"));
            var user = new User { UserName = "admin", Balance = 10000.00 };
            await UserManager.CreateAsync(user, "123456");
            await UserManager.AddToRoleAsync(user, "Root");
        }
    }
}
