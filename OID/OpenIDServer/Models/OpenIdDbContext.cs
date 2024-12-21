using Microsoft.EntityFrameworkCore;

namespace OpenIDServer.Models
{
    public class OpenIdDbContext : DbContext
    {
        public OpenIdDbContext(DbContextOptions<OpenIdDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

    }
}
