using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WechatBribery.Hubs;
using static WechatBribery.Common.Wxpay;

namespace WechatBribery.Controllers
{
    public class WeChatApiController : BaseController
    {
        [Route("[controller]/ExchangeCode/{owner}")]
        public async Task<IActionResult> ExchangeCode(string code, string state, string owner, [FromServices] IHubContext<BriberyHub> Hub)
        {
            try
            {
                var oid = await AuthorizeAsync(code);
                HttpContext.Session.SetString("OpenId", oid.Id);
                HttpContext.Session.SetString("AccessToken", oid.AccessToken);
                HttpContext.Session.SetString("Expire", oid.AccessTokenExpire.ToString());
                HttpContext.Session.SetString("Nickname", oid.NickName);
                HttpContext.Session.SetString("AvatarUrl", oid.AvatarUrl);
                return RedirectToAction("Index", "WeChat", new { owner = owner });
            }
            catch
            {
                return RedirectToAction("Index", "WeChat", new { owner = owner });
            }
        }
    }
}
