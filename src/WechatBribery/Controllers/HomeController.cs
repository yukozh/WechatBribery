using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Pomelo.Data.Excel;
using WechatBribery.Models;
using static Newtonsoft.Json.JsonConvert;

namespace WechatBribery.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Deliver()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Deliver(string Title, string Rules, double Ratio, [FromServices] IHostingEnvironment env)
        {
            if (DB.Activities.Count(x => x.OwnerId == User.Current.Id && !x.End.HasValue) > 0)
                return Prompt(x =>
                {
                    x.Title = "创建失败";
                    x.Details = "还有活动正在进行，请等待活动结束后再创建新活动！";
                    x.StatusCode = 400;
                });
            
            // 检查余额
            var rule = DeserializeObject<List<RuleViewModel>>(Rules);
            if (rule.Count == 0 || rule.Sum(x => x.Count) == 0)
                return Prompt(x =>
                {
                    x.Title = "创建失败";
                    x.Details = "您没有设定红包发放规则";
                });
            if (rule.Any(x => x.From < 100))
                return Prompt(x =>
                {
                    x.Title = "创建失败";
                    x.Details = "每个红包金额最少为1元";
                });
            var total = rule.Sum(x => x.To * x.Count);
            if (total / 100.0 > User.Current.Balance)
                return Prompt(x =>
                {
                    x.Title = "余额不足";
                    x.Details = $"您的余额不足以支付本轮活动的￥{ total.ToString("0.00") }";
                    x.StatusCode = 400;
                });

            // 存储活动信息
            var act = new Activity
            {
                Id = Guid.NewGuid(),
                Begin = DateTime.Now,
                RuleJson = Rules,
                Title = Title,
                Ratio = Ratio / 100.0,
                OwnerId = User.Current.Id
            };

            DB.Activities.Add(act);

            // 创建红包
            var random = new Random();
            foreach (var x in rule)
            {
                for (var i = 0; i < x.Count; i++)
                {
                    DB.Briberies.Add(new Bribery
                    {
                        ActivityId = act.Id,
                        Price = random.Next(x.From, x.To)
                    });
                }
            }
            DB.SaveChanges();

            // 计算红包统计
            act.Price = DB.Briberies.Where(x => x.ActivityId == act.Id).Sum(x => x.Price);
            act.BriberiesCount = DB.Briberies.Count(x => x.ActivityId == act.Id);
            DB.SaveChanges();

            return RedirectToAction("Activity", "Home", new { id = act.Id });
        }

        public IActionResult Activity(Guid id)
        {
            var act = DB.Activities.Single(x => x.Id == id);
            ViewBag.Price = DB.Briberies.Where(x => x.ActivityId == id && x.ReceivedTime.HasValue).Sum(x => x.Price);
            ViewBag.Amount = DB.Briberies.Count(x => x.ActivityId == id && x.ReceivedTime.HasValue);
            ViewBag.Briberies = DB.Briberies
                .Where(x => x.ActivityId == id && x.ReceivedTime.HasValue)
                .OrderByDescending(x => x.ReceivedTime)
                .Take(100);
            return View(act);
        }

        public IActionResult History()
        {
            if (User.IsInRole("Root"))
                return PagedView(DB.Activities.Include(x => x.Owner).OrderByDescending(x => x.Begin));
            else
                return PagedView(DB.Activities.Where(x => x.OwnerId == User.Current.Id).OrderByDescending(x => x.Begin));
        }

        public IActionResult Export(Guid id)
        {
            Activity activity;
            if (User.IsInRole("Root"))
                activity = DB.Activities.Single(x => x.Id == id);
            else
                activity = DB.Activities.Single(x => x.Id == id && x.OwnerId == User.Current.Id);

            var src = DB.Briberies
                .Where(x => x.ActivityId == id && x.ReceivedTime.HasValue)
                .OrderBy(x => x.ReceivedTime)
                .ToList();

            var nonawarded = DB.Briberies
                .Count(x => x.ActivityId == id && !x.ReceivedTime.HasValue);

            var tmp = Guid.NewGuid().ToString();
            var path = Path.Combine(Directory.GetCurrentDirectory(), tmp + ".xlsx");
            using (var excel = ExcelStream.Create(path))
            using (var sheet1 = excel.LoadSheet(1))
            {
                // Headers
                sheet1.Add(new Pomelo.Data.Excel.Infrastructure.Row { "Open Id", "昵称", "金额", "领取时间" });
                foreach(var x in src)
                    sheet1.Add(new Pomelo.Data.Excel.Infrastructure.Row { x.OpenId ?? "", x.NickName ?? "", (x.Price / 100.0).ToString("0.00"), x.ReceivedTime.Value.ToString("yyyy-MM-dd HH:mm:ss") });
                sheet1.Add(new Pomelo.Data.Excel.Infrastructure.Row());
                sheet1.Add(new Pomelo.Data.Excel.Infrastructure.Row { "未领取金额（元）", "未领取红包（个）", "总参与人数" });
                sheet1.Add(new Pomelo.Data.Excel.Infrastructure.Row { ((activity.Price - src.Sum(x => x.Price)) / 100.0).ToString("0.00"), nonawarded.ToString(), activity.Attend.ToString() });
                sheet1.SaveChanges();
            }
            var ret = System.IO.File.ReadAllBytes(path);
            System.IO.File.Delete(path);
            return File(ret, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", activity.Title + ".xlsx");
        }

        [HttpPost]
        public IActionResult Stop(Guid id)
        {
            Activity act;
            if (User.IsInRole("Root"))
            {
                act = DB.Activities
                    .Include(x => x.Owner)
                    .Single(x => x.Id == id);
            }
            else
            {
                act = DB.Activities
                    .Include(x => x.Owner)
                    .Single(x => x.Id == id && x.OwnerId == User.Current.Id);
            }
            act.End = DateTime.Now;
            DB.SaveChanges();
            if (WeChatController.dic.ContainsKey(act.Owner.UserName))
                WeChatController.dic.Remove(act.Owner.UserName);
            return RedirectToAction("Activity", "Home", new { id = id });
        }

        public IActionResult AttendCount(Guid id)
        {
            var activity = DB.Activities.Single(x => x.Id == id);
            return Content(activity.Attend.ToString());
        }
    }
}
