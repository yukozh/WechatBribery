using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WechatBribery.Models
{
    public class Bribery
    {
        public Guid Id { get; set; }

        public Guid ActivityId { get; set; }

        public virtual Activity Activity { get; set; }

        /// <summary>
        /// 发放时间
        /// </summary>
        public DateTime? DeliverTime { get; set; }

        /// <summary>
        /// 领取时间
        /// </summary>
        public DateTime? ReceivedTime { get; set; }

        /// <summary>
        /// 以分为单位
        /// </summary>
        public long Price { get; set; }

        public string OpenId { get; set; }

        public string AvatarUrl { get; set; }

        public string NickName { get; set; }
    }
}
