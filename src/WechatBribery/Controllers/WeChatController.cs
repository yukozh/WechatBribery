using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace WechatBribery.Controllers
{
    public class WeChatController : BaseController
    {
        public IActionResult Index()
        {
            var activity = DB.Activities.LastOrDefault();
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
    }
}
