using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WechatBribery.Models
{
    public class RuleViewModel
    {
        /// <summary>
        /// 以分为单位
        /// </summary>
        public int From { get; set; }

        /// <summary>
        /// 以分为单位
        /// </summary>
        public int To { get; set; }

        public double Ratio { get; set; }

        public long Count { get; set; }
    }
}
