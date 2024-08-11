using OnlineTicketingSystem.DAL;

namespace OnlineTicketingSystem.Services
{
    public interface ITicketService
    {
        Task<(bool Success, Ticket? Ticket)> TryBookTicketAsync(int ticketId, string userId);
        Task<(bool Success, Ticket? Ticket)> TryBookTicketUsingLockAsync(int ticketId, string userId);
    }
}