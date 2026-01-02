using Entities.DB.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.DB.DbContexts;

public abstract class CommonContext : DbContext
{
    public CommonContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
}
