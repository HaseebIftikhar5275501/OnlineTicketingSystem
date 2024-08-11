using Microsoft.EntityFrameworkCore;

namespace OnlineTicketingSystem.DAL
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<Ticket> Tickets { get; set; }
    }

    public class Ticket
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UserId { get; set; }
        public bool IsBooked { get; set; }
    }
}
