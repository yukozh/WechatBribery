using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Security.Cryptography;
using WechatBribery.Models;
using static Newtonsoft.Json.JsonConvert;

namespace WechatBribery.Common
{
    public static class Wxpay
    {
        public static async Task<OpenId> AuthorizeAsync(string code)
        {
            using (var client = new HttpClient { BaseAddress = new Uri("https://api.weixin.qq.com") })
            {
                var result = await client.GetAsync($"/sns/oauth2/access_token?appid={ Startup.Config["WeChat:AppId"] }&secret={ Startup.Config["WeChat:Secret"] }&code={ code }&grant_type=authorization_code");
                var jsonStr = await result.Content.ReadAsStringAsync();
                var json = DeserializeObject<dynamic>(jsonStr);
                var ret = new OpenId();
                ret.AccessToken = json.access_token;
                ret.AccessTokenExpire = DateTime.Now.AddSeconds(json.expires_in);
                ret.RefreshToken = json.refresh_token;
                ret.AccessTokenExpire = DateTime.Now.AddDays(30);
                ret.Id = json.openid;
                return ret;
            }
        }

        public static async Task<bool> TransferMoneyAsync(Guid DeliverId, string OpenId, double Price, string Description)
        {
            var nounce = Guid.NewGuid().ToString().Replace("-", "");
            var requestUrl = "";
            var dic = new Dictionary<string, string>();
            dic.Add("mch_appid", Startup.Config["WeChat:AppId"]);
            dic.Add("mchid", Startup.Config["WeChat:MchId"]);
            dic.Add("nonce_str", nounce);
            dic.Add("partner_trade_no", DeliverId.ToString());
            dic.Add("openid", OpenId);
            dic.Add("amount", Price.ToString());
            dic.Add("desc", Description);
            dic.Add("spbill_create_ip", Startup.Config["Ip"]);
            foreach(var x in dic.OrderBy(x => x.Key))
                requestUrl += x.Key + "=" + x.Value + "&";
            requestUrl += "key=" + Startup.Config["WeChat:SignKey"];
            string requestXml;

            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.UTF8.GetBytes(requestUrl));
                var strResult = Encoding.ASCII.GetString(result);
                requestXml = $@"<xml> 
<mch_appid>{ Startup.Config["WeChat:AppId"] }</mch_appid> 
<mchid>{ Startup.Config["WeChat:MchId"] }</mchid> 
<nonce_str>{ nounce }</nonce_str> 
<partner_trade_no>{ DeliverId }</partner_trade_no> 
<openid>{ OpenId }</openid> 
<amount>{ Price }</amount> 
<desc>{ Description }</desc> 
<spbill_create_ip>{ Startup.Config["Ip"] }</spbill_create_ip> 
<sign>{ result }</sign> 
</xml>";
            }


            using (var client = new HttpClient() { BaseAddress = new Uri("https://api.mch.weixin.qq.com") })
            {
                var result = await client.PostAsync("/mmpaymkttransfers/promotion/transfers", new ByteArrayContent(Encoding.UTF8.GetBytes(requestXml)));
                var html = await result.Content.ReadAsStringAsync();
                if (html.IndexOf("SUCCESS") >= 0)
                    return true;
                return false;
            }
        }

    }
}
