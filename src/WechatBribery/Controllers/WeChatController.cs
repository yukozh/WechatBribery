using Microsoft.AspNetCore.Mvc;

namespace WechatBribery.Controllers
{
    public class WeChatController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
