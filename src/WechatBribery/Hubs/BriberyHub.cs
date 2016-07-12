using Microsoft.AspNetCore.SignalR;

namespace WechatBribery.Hubs
{
    public class BriberyHub : Hub
    {
        public void Join(string name)
        {
            Groups.Add(Context.ConnectionId, name);
        }
    }
}
