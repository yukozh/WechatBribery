using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WechatBribery.Models
{
    public class PayLog
    {
        public Guid Id { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }

        public virtual User User { get; set; }

        public double Price { get; set; }

        public double Current { get; set; }

        public DateTime Time { get; set; }
    }
}
