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
    }
}
