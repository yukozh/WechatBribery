using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Pomelo.AspNetCore.TimedJob;
using WechatBribery.Models;
using WechatBribery.Hubs;

namespace WechatBribery.Jobs
{
    public class AttendCountPushJob : Job
    {
        [Invoke(Interval = 3000)]
        public void Push(IHubContext<BriberyHub> Hub, BriberyContext DB)
        {
            try
            {
                var needPush = DB.Activities.Where(x => !x.End.HasValue).ToList();
                DB.ChangeTracker.DetectChanges();
                foreach (var x in needPush)
                {
                    try
                    {
                        Hub.Clients.Group(x.Id.ToString()).OnShaked(x.Attend);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
