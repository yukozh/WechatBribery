using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WechatBribery.Hubs;
using static WechatBribery.Common.Wxpay;

namespace WechatBribery.Controllers
{
    public class WeChatApiController : BaseController
    {
        [Route("[controller]/ExchangeCode/{dtoken}")]
        public async Task<IActionResult> ExchangeCode(string code, string state, string dtoken, [FromServices] IHubContext<BriberyHub> Hub)
        {
            // 1. Getting authorization informations
            var oid = await AuthorizeAsync(code);
            if (DB.OpenIds.Count(x => x.Id == oid.Id) == 0)
            {
                DB.OpenIds.Add(oid);
            }
            else
            {
                var _oid = DB.OpenIds.Single(x => x.Id == oid.Id);
                _oid.AccessToken = oid.AccessToken;
                _oid.AccessTokenExpire = oid.AccessTokenExpire;
                _oid.RefreshToken = oid.RefreshToken;
                _oid.RefreshTokenExpire = oid.RefreshTokenExpire;
                _oid.NickName = oid.NickName;
                _oid.AvatarUrl = oid.AvatarUrl;
            }
            DB.SaveChanges();

            // 2. Find deliver informations
            var prize = DB.Briberies.Single(x => x.Id == Guid.Parse(dtoken));
            if (prize.ReceivedTime.HasValue)
                return Content("Error");

            // 3. Check deliver count
            var beg = DateTime.Now.Date;
            if (DB.Briberies.Count(x => x.OpenId == oid.Id && x.ReceivedTime.HasValue && x.ReceivedTime.Value >= beg) >= 10)
                return View("Exceeded");

            // 4. Update deliver informations
            await TransferMoneyAsync(prize.Id, oid.Id, prize.Price, Startup.Config["WeChat:TransferDescription"]);
            prize.ReceivedTime = DateTime.Now;
            prize.OpenId = oid.Id;
            prize.NickName = oid.NickName;
            prize.AvatarUrl = oid.AvatarUrl;
            DB.SaveChanges();

            // 5. Push deliver information to administrators
            Hub.Clients.Group(prize.ActivityId.ToString()).OnDelivered(new { time = prize.ReceivedTime, avatar = oid.AvatarUrl, name = oid.NickName, price = prize.Price, id = oid.Id });

            return View("Bribery", prize);
        }
    }
}
