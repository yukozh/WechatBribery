using System;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using WechatBribery.Models;

namespace WechatBribery.Hubs
{
    public class BriberyHub : Hub
    {
        public string Shake()
        {
            // 获取活动信息
            var DB = Context.Request.HttpContext.RequestServices.GetRequiredService<BriberyContext>();
            var activity = DB.Activities.Where(x => x.Begin <= DateTime.Now && !x.End.HasValue).FirstOrDefault();
            if (activity == null)
                return "NO";

            // 参与人数缓存
            var Cache = Context.Request.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
            long cnt;
            if (!Cache.TryGetValue(activity.Id, out cnt))
                cnt = 0;
            cnt++;
            Cache.Set(activity.Id, cnt);
            Clients.Group(activity.Id.ToString()).OnShaked();

            // 抽奖
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
                    activity.Attend = cnt;
                    DB.SaveChanges();
                    Clients.Group(activity.Id.ToString()).OnActivityEnd();
                    return "RETRY";
                }

                prize.DeliverTime = DateTime.Now;
                DB.SaveChanges();

                if (DB.Briberies.Count(x => x.ActivityId == activity.Id && !x.DeliverTime.HasValue) == 0)
                {
                    activity.End = DateTime.Now;
                    activity.Attend = cnt;
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
