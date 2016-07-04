using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
        public IActionResult Deliver(string Title, string Rules, double Ratio, IFormFile Background, IFormFile Bottom, string BottomUrl, [FromServices] IHostingEnvironment env)
        {
            // 存储活动信息
            var act = new Activity
            {
                Id = Guid.NewGuid(),
                Begin = DateTime.Now,
                RuleJson = Rules,
                Title = Title,
                Ratio = Ratio / 100.0,
                BottomUrl = BottomUrl
            };

            if (Background != null && Background.Length > 0)
                act.Background = Background.ReadAllBytes();
            else
                act.Background = System.IO.File.ReadAllBytes(Path.Combine(env.WebRootPath, "images", "main.png"));

            if (Bottom != null && Bottom.Length > 0)
                act.Bottom = Bottom.ReadAllBytes();
            else
                act.Bottom = System.IO.File.ReadAllBytes(Path.Combine(env.WebRootPath, "images", "bottom.png"));

            DB.Activities.Add(act);
            DB.SaveChanges();

            // 创建红包
            var rule = DeserializeObject<List<Rule>>(Rules);
            var random = new Random();
            foreach(var x in rule)
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
            return PagedView(DB.Activities.OrderByDescending(x => x.Begin));
        }

        public IActionResult Export(Guid id)
        {
            var activity = DB.Activities.Single(x => x.Id == id);

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

        public IActionResult Background(Guid id)
        {
            return File(DB.Activities.Single(x => x.Id == id).Background, "image/png");
        }

        public IActionResult Bottom(Guid id)
        {
            return File(DB.Activities.Single(x => x.Id == id).Bottom, "image/png");
        }

        [HttpPost]
        public IActionResult Stop(Guid id)
        {
            var act = DB.Activities.Single(x => x.Id == id);
            act.End = DateTime.Now;
            DB.SaveChanges();
            return RedirectToAction("Activity", "Home", new { id = id });
        }
    }
}
