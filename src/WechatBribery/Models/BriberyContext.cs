using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Pomelo.AspNetCore.Extensions.BlobStorage.Models;

namespace WechatBribery.Models
{
    public class BriberyContext : IdentityDbContext<User>, IBlobStorageDbContext
    {
        public BriberyContext(DbContextOptions opt)
            : base(opt)
        {
        }

        public DbSet<PayLog> PayLogs { get; set; }

        public DbSet<Blob> Blobs { get; set; }

        public DbSet<Bribery> Briberies { get; set; }

        public DbSet<Activity> Activities { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.HasPostgresExtension("uuid-ossp");

            builder.Entity<Bribery>(e =>
            {
                e.HasIndex(x => x.ReceivedTime);
                e.HasIndex(x => x.Price);
            });

            builder.Entity<PayLog>(e =>
            {
                e.HasIndex(x => x.Price);
                e.HasIndex(x => x.Time);
            });

            builder.Entity<Activity>(e =>
            {
                e.HasIndex(x => x.Price);
                e.HasIndex(x => x.Begin);
                e.HasIndex(x => x.End);
            });

            builder.Entity<Bribery>(e =>
            {
                e.HasIndex(x => x.ReceivedTime);
            });
        }
    }
}
