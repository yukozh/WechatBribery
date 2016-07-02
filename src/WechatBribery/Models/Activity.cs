using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WechatBribery.Models
{
    public class Activity
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string RuleJson { get; set; }

        public DateTime Begin { get; set; }

        public DateTime? End { get; set; }

        public double Ratio { get; set; }

        public long BriberiesCount { get; set; }

        public long Price { get; set; }
    }
}
