using System;
using System.Collections.Generic;
using System.Linq;
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
            var UserManager = services.GetRequiredService<UserManager<IdentityUser>>();
            DB.Database.EnsureCreated();
            await UserManager.CreateAsync(new IdentityUser { UserName = "admin" }, "123456");
        }
    }
}
