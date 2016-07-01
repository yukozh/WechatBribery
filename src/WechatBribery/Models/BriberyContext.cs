using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace WechatBribery.Models
{
    public class BriberyContext : IdentityDbContext<IdentityUser>
    {
        public BriberyContext(DbContextOptions opt)
            : base(opt)
        {

        }

        public DbSet<OpenId> OpenIds { get; set; }

        public DbSet<Delivery> Deliveries { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Delivery>(e =>
            {
                e.HasIndex(x => x.Time);
                e.HasIndex(x => x.Price);
                e.HasIndex(x => x.IsReceived);
            });
        }
    }
}
