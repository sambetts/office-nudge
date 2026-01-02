
using Entities.DB.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.DB.DbContexts;


public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public async Task<bool> EnsureCreated()
    {
        return await Database.EnsureCreatedAsync();
    }
}

