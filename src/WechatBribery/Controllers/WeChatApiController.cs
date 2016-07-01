using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using static WechatBribery.Common.Wxpay;

namespace WechatBribery.Controllers
{
    public class WeChatApiController : BaseController
    {
        [Route("{controller}/ExchangeCode/{dtoken}")]
        public async Task<IActionResult> ExchangeCode(string code, string state, string dtoken)
        {
            // 1. Getting authorization informations
            var oid = await AuthorizeAsync(code);
            DB.OpenIds.Add(oid);
            DB.SaveChanges();

            // 2. Find deliver informations
            var deliver = DB.Deliveries.Single(x => x.Token == dtoken);
            if (deliver.IsReceived)
                return Content("Error");

            // 3. Update deliver informations
            var flag = await TransferMoneyAsync(deliver.Id, oid.Id, deliver.Price, Startup.Config["WeChat:TransferDescription"]);
            if (flag)
                deliver.IsReceived = true;
            DB.SaveChanges();

            return View();
        }
    }
}
