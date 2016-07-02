using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WechatBribery.Models;
using static Newtonsoft.Json.JsonConvert;
using System.Threading.Tasks;

namespace WechatBribery.Hubs
{
    public class BriberyHub : Hub
    {
        public string Shake()
        {
            var DB = Context.Request.HttpContext.RequestServices.GetRequiredService<BriberyContext>();
            var activity = DB.Activities.Where(x => x.Begin <= DateTime.Now && !x.End.HasValue).FirstOrDefault();
            if (activity == null)
                return "NO";

            var rand = new Random();
            var num = rand.Next(0, 10000);

            if (num <= activity.Ratio * 10000) // 中奖
            {
                var prize = DB.Briberies
                    .Where(x => x.ActivityId == activity.Id && !x.DeliverTime.HasValue)
                    .OrderBy(x => Guid.NewGuid())
                    .FirstOrDefault();
                if (prize == null) // 没有红包了
                {
                    activity.End = DateTime.Now;
                    DB.SaveChanges();
                    Clients.Group(activity.Id.ToString()).OnActivityEnd();
                    return "RETRY";
                }

                prize.DeliverTime = DateTime.Now;
                DB.SaveChanges();

                if (DB.Briberies.Count(x => x.ActivityId == activity.Id && !x.DeliverTime.HasValue) == 0)
                {
                    activity.End = DateTime.Now;
                    DB.SaveChanges();
                    Clients.Group(activity.Id.ToString()).OnActivityEnd();
                }

                return prize.Id.ToString();
            }

            return "RETRY";
        }

        public void Join(string name)
        {
            Groups.Add(Context.ConnectionId, name);
        }
    }
}
