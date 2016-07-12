using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pomelo.AspNetCore.Extensions.BlobStorage.Models;
using WechatBribery.Models;

namespace WechatBribery.Controllers
{
    public class AccountController : BaseController
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<string> Login(string username, string password)
        {
            var result = await SignInManager.PasswordSignInAsync(username, password, false, false);
            if (result.Succeeded)
                return "success";
            else
                return "error";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignOut()
        {
            await SignInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(IFormFile top, IFormFile bottom, string url)
        {
            if (top != null)
            {
                var topblob = new Blob
                {
                    Id = Guid.NewGuid(),
                    Time = DateTime.Now,
                    Bytes = top.ReadAllBytes(),
                    ContentType = top.ContentType,
                    ContentLength = top.Length,
                    FileName = top.FileName
                };
                User.Current.TopPictureId = topblob.Id;
                DB.Blobs.Add(topblob);
            }
            if (bottom != null)
            {
                var bottomblob = new Blob
                {
                    Id = Guid.NewGuid(),
                    Time = DateTime.Now,
                    Bytes = top.ReadAllBytes(),
                    ContentType = top.ContentType,
                    ContentLength = top.Length,
                    FileName = top.FileName
                };
                User.Current.BottomPictureId = bottomblob.Id;
                DB.Blobs.Add(bottomblob);
            }
            User.Current.ActivityURL = url;
            DB.SaveChanges();
            return Prompt(x =>
            {
                x.Title = "修改成功";
                x.Details = "商户信息已更新！";
            });
        }

        [HttpGet]
        public IActionResult Password()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Password(string old, string @new, string confirm)
        {
            if (@new != confirm)
                return Prompt(x =>
                {
                    x.Title = "修改失败";
                    x.Details = "两次密码不一致";
                });
            var result = await User.Manager.ChangePasswordAsync(User.Current, old, @new);
            if (result.Succeeded)
                return Prompt(x =>
                {
                    x.Title = "修改成功";
                    x.Details = "新密码已经生效！";
                });
            else
                return Prompt(x =>
                {
                    x.Title = "修改失败";
                    x.Details = result.Errors.First().Description;
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await SignInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [Authorize(Roles = "Root")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Root")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(double balance, string username, string password, string role)
        {
            var user = new User { UserName = username, Balance = balance };
            await UserManager.CreateAsync(user, password);
            await UserManager.AddToRoleAsync(user, role);
            if (balance > 0)
            {
                DB.PayLogs.Add(new PayLog
                {
                    Current = balance,
                    Price = balance,
                    Time = DateTime.Now,
                    UserId = user.Id
                });
                DB.SaveChanges();
            }
            return Prompt(x => 
            {
                x.Title = "创建成功";
                x.Details = $"用户{ user.UserName }已经成功创建";
            });
        }

        [HttpGet]
        public IActionResult Index()
        {
            var ret = UserManager.Users.ToList();
            return View(ret);
        }

        [HttpGet]
        [Authorize(Roles = "Root")]
        public async Task<IActionResult> Charge(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            return View(user);
        }

        [HttpPost]
        [Authorize(Roles = "Root")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Charge(string id, double price)
        {
            var user = await UserManager.FindByIdAsync(id);
            lock(this)
            {
                user.Balance += price;
                DB.PayLogs.Add(new PayLog { UserId = User.Current.Id, Price = price, Time = DateTime.Now, Current = user.Balance });
                DB.SaveChanges();
            }
            return Prompt(x =>
            {
                x.Title = "充值成功";
                x.Details = $"本次为 { user.UserName } 充入了 ￥{ price.ToString("0.00") }";
            });
        }


        [HttpGet]
        [Authorize(Roles = "Root")]
        public async Task<IActionResult> ResetPwd(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            return View(user);
        }

        [HttpPost]
        [Authorize(Roles = "Root")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPwd(string id, string pwd)
        {
            var user = await UserManager.FindByIdAsync(id);
            var token = await UserManager.GeneratePasswordResetTokenAsync(user);
            await UserManager.ResetPasswordAsync(user, token, pwd);
            return Prompt(x =>
            {
                x.Title = "修改成功";
                x.Details = $"{ user.UserName }的密码已经被重置成为了{ pwd }";
            });
        }
    }
}
