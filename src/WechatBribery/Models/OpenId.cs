using System;
using System.ComponentModel.DataAnnotations;

namespace WechatBribery.Models
{
    public class OpenIdViewModel
    {
        [MaxLength(128)]
        public string Id { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public DateTime AccessTokenExpire { get; set; }

        public DateTime RefreshTokenExpire { get; set; }

        public string NickName { get; set; }

        public string AvatarUrl { get; set; }
    }
}
