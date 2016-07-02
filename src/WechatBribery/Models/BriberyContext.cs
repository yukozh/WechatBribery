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

        public DbSet<Bribery> Briberies { get; set; }

        public DbSet<Activity> Activities { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Bribery>(e =>
            {
                e.HasIndex(x => x.DeliverTime);
                e.HasIndex(x => x.ReceivedTime);
                e.HasIndex(x => x.Price);
            });
        }
    }
}
