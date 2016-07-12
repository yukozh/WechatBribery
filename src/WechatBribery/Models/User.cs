using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Pomelo.AspNetCore.Extensions.BlobStorage.Models;

namespace WechatBribery.Models
{
    public class User : IdentityUser
    {
        public double Balance { get; set; }

        [ForeignKey("TopPicture")]
        public Guid? TopPictureId { get; set; }

        public virtual Blob TopPicture { get; set; }

        [ForeignKey("BottomPicture")]
        public Guid? BottomPictureId { get; set; }

        public virtual Blob BottomPicture { get; set; }

        [MaxLength(256)]
        public string ActivityURL { get; set; }
    }
}
