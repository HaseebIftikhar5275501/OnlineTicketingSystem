using Microsoft.EntityFrameworkCore;
using OnlineTicketingSystem.DAL;

namespace OnlineTicketingSystem.Services
{
    public class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _context;

        public TicketService(ApplicationDbContext context)
        {
            _context = context;
        }


       
        public async Task<(bool Success, Ticket? Ticket)> TryBookTicketAsync(int ticketId, string userId)
        {
            // Start a transaction with a specific isolation level if needed
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                // Acquire a lock on the ticket row to prevent other transactions from modifying it
                var ticket = await _context.Tickets
                    .FromSqlRaw("SELECT * FROM Tickets WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", ticketId)
                    .SingleOrDefaultAsync();

                if (ticket == null)
                {
                    await transaction.RollbackAsync();
                    return (false, null);
                }

                if (ticket.IsBooked)
                {
                    await transaction.RollbackAsync();
                    return (false, ticket);
                }

                // Mark the ticket as booked
                ticket.IsBooked = true;
                ticket.UserId = userId;
                await _context.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                return (true, ticket);
            }
            catch
            {
                // Rollback in case of an exception
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static readonly object _ticketLock = new object();
        private static bool _bookingInProgress = false;

        public async Task<(bool Success, Ticket? Ticket)> TryBookTicketUsingLockAsync(int ticketId, string userId)
        {
            Ticket? ticket = null;
            bool success = false;

            lock (_ticketLock)
            {
                // Wait until no booking is in progress
                while (_bookingInProgress)
                {
                    Monitor.Wait(_ticketLock);
                }

                // Set booking in progress flag
                _bookingInProgress = true;

                // Fetch the ticket from the database
                ticket = _context.Tickets
                    .Where(t => t.Id == ticketId)
                    .SingleOrDefault();

                if (ticket == null)
                {
                    // Ticket not found
                    _bookingInProgress = false;
                    Monitor.Pulse(_ticketLock);
                    return (false, null);
                }

                if (ticket.IsBooked)
                {
                    // Ticket already booked
                    _bookingInProgress = false;
                    Monitor.Pulse(_ticketLock);
                    return (false, ticket);
                }

                // Mark the ticket as booked
                ticket.IsBooked = true;
                ticket.UserId = userId;
                _context.SaveChanges(); // Save changes synchronously here

                success = true;

                // Notify other waiting threads
                Monitor.PulseAll(_ticketLock);
                _bookingInProgress = false;
            }

            await Task.CompletedTask;
            return (success, ticket);
        }


        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public async Task<(bool Success, Ticket? Ticket)> TryBookTicketUsingSlimSemaphoreAsync(int ticketId, string userId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var ticket = await _context.Tickets
                     .FromSqlRaw("SELECT * FROM Tickets WHERE Id = {0}", ticketId)
                     .SingleOrDefaultAsync();
                if (ticket == null || ticket.IsBooked)
                {
                    return (false, null);
                }

                // Mark the ticket as booked
                ticket.IsBooked = true;
                ticket.UserId = userId;
                await _context.SaveChangesAsync();
                return (true, ticket);
            }
            finally
            {
                _semaphore.Release();
            }
        }

    }
}