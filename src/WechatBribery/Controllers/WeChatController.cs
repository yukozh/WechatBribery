using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WechatBribery.Hubs;
using static WechatBribery.Common.Wxpay;

namespace WechatBribery.Controllers
{
    public class WeChatController : BaseController
    {
        public IActionResult Index(string id)
        {
            // 判断是否需要授权
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("OpenId")) || Convert.ToDateTime(HttpContext.Session.GetString("Expire")) <= DateTime.Now)
                return Redirect("https://open.weixin.qq.com/connect/oauth2/authorize?appid=" + Startup.Config["WeChat:AppId"] + "&redirect_uri=" + HttpContext.Request.Scheme + "://" + HttpContext.Request.Host + "/WeChatApi/ExchangeCode/" + id + "&response_type=code&scope=snsapi_userinfo");

            var user = DB.Users.Single(x => x.UserName == id);
            var activity = DB.Activities
                .Include(x => x.Owner)
                .Where(x => x.Owner.UserName == user.Id)
                .LastOrDefault();
            return View(activity);
        }

        public IActionResult History()
        {
            var activity = DB.Activities.Last();
            var ret = DB.Briberies
                .Where(x => x.ActivityId == activity.Id && x.ReceivedTime.HasValue)
                .OrderByDescending(x => x.ReceivedTime)
                .Take(10)
                .ToList();
            return View(activity);
        }

        public IActionResult Exceeded()
        {
            return View();
        }

        public async Task<IActionResult> Shake(string id, [FromServices] IHubContext<BriberyHub> Hub)
        {
            // 判断是否需要授权
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("OpenId")) || Convert.ToDateTime(HttpContext.Session.GetString("Expire")) <= DateTime.Now)
                return Content("AUTH");
            
            // 微信平台要求15秒内不能给同一个用户再次发红包
            var cooldown = DateTime.Now.AddSeconds(-15);
            if (DB.Briberies.Count(x => x.OpenId == HttpContext.Session.GetString("OpenId") && x.ReceivedTime >= cooldown) > 0)
                return Content("RETRY");

            // 判断是否中奖超过10次
            var beg = DateTime.Now.Date; 
            if (DB.Briberies.Count(x => x.OpenId == HttpContext.Session.GetString("OpenId") && x.ReceivedTime.HasValue && x.ReceivedTime.Value >= beg) >= 10)
                return Content("EXCEEDED");

            // 获取活动信息
            var activity = DB.Activities
                .Include(x => x.Owner)
                .FirstOrDefault(x => x.Owner.UserName == id && !x.End.HasValue);
            if (activity == null)
                return Content("NO");

            // 参与人数缓存
            activity.Attend++;
            DB.SaveChanges();
            if (activity.Attend % 600 == 0)
                GC.Collect();
            Hub.Clients.Group(activity.Id.ToString()).OnShaked();

            // 抽奖
            var rand = new Random();
            var num = rand.Next(0, 10000);
            if (num <= activity.Ratio * 10000)
            {
                var prize = DB.Briberies
                    .Where(x => x.ActivityId == activity.Id && !x.ReceivedTime.HasValue)
                    .OrderBy(x => Guid.NewGuid())
                    .FirstOrDefault();
                if (prize == null) // 没有红包了
                {
                    activity.End = DateTime.Now;
                    DB.SaveChanges();
                    Hub.Clients.Group(activity.Id.ToString()).OnActivityEnd();
                    return Content("RETRY");
                }

                // 中奖发放红包
                prize.OpenId = HttpContext.Session.GetString("OpenId");
                prize.NickName = HttpContext.Session.GetString("Nickname");
                prize.AvatarUrl = HttpContext.Session.GetString("AvatarUrl");
                prize.ReceivedTime = DateTime.Now;
                DB.SaveChanges();

                // 微信转账
                await TransferMoneyAsync(prize.Id, HttpContext.Session.GetString("OpenId"), prize.Price, Startup.Config["WeChat:TransferDescription"]);
                Hub.Clients.Group(prize.ActivityId.ToString()).OnDelivered(new { time = prize.ReceivedTime, avatar = HttpContext.Session.GetString("AvatarUrl"), name = HttpContext.Session.GetString("Nickname"), price = prize.Price, id = HttpContext.Session.GetString("OpenId") });

                try
                {
                    DB.ChangeTracker.DetectChanges();
                    // 检查剩余红包数
                    if (DB.Briberies.Count(x => x.ActivityId == activity.Id && !x.ReceivedTime.HasValue) == 0)
                    {
                        activity.End = DateTime.Now;
                        DB.SaveChanges();
                        Hub.Clients.Group(activity.Id.ToString()).OnActivityEnd();
                    }
                }
                catch { }

                // 扣除红包费用
                activity.Owner.Balance -= prize.Price / 100.0;
                DB.ChangeTracker.DetectChanges();
                DB.SaveChanges();

                // 返回中奖信息
                return Content((prize.Price / 100.0).ToString("0.00"));
            }

            return Content("RETRY");
        }

        public IActionResult Bribery(Guid id)
        {
            var act = DB.Activities.Single(x => x.Id == id);
            return View(act);
        }
    }
}
