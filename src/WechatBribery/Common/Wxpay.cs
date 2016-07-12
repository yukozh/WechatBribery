using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using WechatBribery.Models;
using static Newtonsoft.Json.JsonConvert;

namespace WechatBribery.Common
{
    public static class Wxpay
    {
        public static async Task<OpenIdViewModel> AuthorizeAsync(string code)
        {
            using (var client = new HttpClient { BaseAddress = new Uri("https://api.weixin.qq.com") })
            {
                // 获取用户基本信息
                var result = await client.GetAsync($"/sns/oauth2/access_token?appid={ Startup.Config["WeChat:AppId"] }&secret={ Startup.Config["WeChat:Secret"] }&code={ code }&grant_type=authorization_code");
                var jsonStr = await result.Content.ReadAsStringAsync();
                var json = DeserializeObject<dynamic>(jsonStr);
                var ret = new OpenIdViewModel();
                ret.AccessToken = json.access_token;
                ret.AccessTokenExpire = DateTime.Now.AddSeconds((int)json.expires_in);
                ret.RefreshToken = json.refresh_token;
                ret.AccessTokenExpire = DateTime.Now.AddDays(30);
                ret.Id = json.openid;

                // 获取用户头像及昵称
                var result2 = await client.GetAsync($"/sns/userinfo?access_token={ ret.AccessToken }&openid={ ret.Id }&lang=zh_CN");
                var jsonStr2 = await result2.Content.ReadAsStringAsync();
                var json2 = DeserializeObject<dynamic>(jsonStr2);
                ret.AvatarUrl = json2.headimgurl;
                ret.NickName = json2.nickname;

                return ret;
            }
        }

        public static async Task<bool> TransferMoneyAsync(Guid DeliverId, string OpenId, long Price, string Description)
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
            dic.Add("check_name", "NO_CHECK");
            foreach (var x in dic.OrderBy(x => x.Key))
                requestUrl += x.Key + "=" + x.Value + "&";
            requestUrl += "key=" + Startup.Config["WeChat:SignKey"];
            string requestXml;

            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.UTF8.GetBytes(requestUrl));
                var strResult = ToHex(result, true);
                requestXml = $@"<xml> 
<mch_appid>{ Startup.Config["WeChat:AppId"] }</mch_appid> 
<mchid>{ Startup.Config["WeChat:MchId"] }</mchid> 
<nonce_str>{ nounce }</nonce_str> 
<partner_trade_no>{ DeliverId }</partner_trade_no> 
<openid>{ OpenId }</openid> 
<amount>{ Price }</amount> 
<desc>{ Description }</desc> 
<spbill_create_ip>{ Startup.Config["Ip"] }</spbill_create_ip> 
<check_name>NO_CHECK</check_name>
<sign>{ strResult }</sign> 
</xml>";
            }

            var handler = new HttpClientHandler();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "apiclient_cert.p12");
            handler.ClientCertificates.Add(new System.Security.Cryptography.X509Certificates.X509Certificate2(path, Startup.Config["WeChat:MchId"]));
            using (var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.mch.weixin.qq.com") })
            {
                var result = await client.PostAsync("/mmpaymkttransfers/promotion/transfers", new ByteArrayContent(Encoding.UTF8.GetBytes(requestXml)));
                var html = await result.Content.ReadAsStringAsync();
                if (html.IndexOf("ERROR") == 0)
                    return true;
                return false;
            }
        }

        public static string ToHex(this byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }
    }
}
