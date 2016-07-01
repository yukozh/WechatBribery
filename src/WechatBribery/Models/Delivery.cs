using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace WechatBribery.Models
{
    public class Delivery
    {
        public Guid Id { get; set; }

        [MaxLength(64)]
        public string Token { get; set; }

        public double Price { get; set; }

        public DateTime Time { get; set; }

        public string Hint { get; set; }

        public bool IsReceived { get; set; }
    }
}
