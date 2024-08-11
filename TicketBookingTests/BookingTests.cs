using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineTicketingSystem.DAL;
using OnlineTicketingSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketBookingTests
{
    public class BookingTests
    {
        private readonly IServiceScopeFactory _scopeFactory;
        // Constructor for dependency injection in unit tests
        public BookingTests()
        {

            var services = new ServiceCollection();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer("data source=localhost;initial catalog=TicketingSystem;User ID=sa;Password=123456;MultipleActiveResultSets=True;connection timeout=300;App=EntityFramework; Integrated Security=true;Trusted_Connection=True;TrustServerCertificate=True;");
            });

            // Register services
            services.AddScoped<ITicketService, TicketService>();

            // Build service provider and get scope factory
            var serviceProvider = services.BuildServiceProvider();
            _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        }

        [Fact]
        public async Task BookTicketAsync_ShouldBookSuccessfully_WhenTicketIsAvailable()
        {

            using (var scopeFactory = _scopeFactory.CreateScope())
            {
                var _ticketService = scopeFactory.ServiceProvider.GetRequiredService<ITicketService>();
                // Arrange
                int ticketId = 3;
                string userId = Guid.NewGuid().ToString();
                // Act
                var result = await _ticketService.TryBookTicketAsync(ticketId, userId);

                // Assert
                Assert.NotNull(result.Ticket); // return ticket
                Assert.True(result.Success);
            }
        }

        [Fact]
        public async Task BookingConcurrency_ShouldOnlyAllowOneSuccessfulBooking()
        {
            var ticketId = 6;
            var tasks = new List<Task>();
            var successfulBookings = 0;
            string userId = string.Empty;
            List<ResponseModel> Result = new List<ResponseModel>();
            // Act
            for (int i = 0; i < 10; i++)
            {

                tasks.Add(Task.Run(async () =>
                {
                    userId = Guid.NewGuid().ToString();
                    using (var scopeFactory = _scopeFactory.CreateScope())
                    {
                        var _ticketService = scopeFactory.ServiceProvider.GetRequiredService<ITicketService>();
                        var (success, bookedTicket) = await _ticketService.TryBookTicketAsync(ticketId, userId);
                        Result.Add(new ResponseModel
                        {
                            Success = success,
                            Ticket= bookedTicket,
                            UserId = userId
                        });
                        if (success)
                        {
                            successfulBookings++;
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.All(tasks, task => Assert.True(task.IsCompletedSuccessfully));
            Assert.Equal(0, successfulBookings); // Ticket already booked and not available for further booking
            Assert.Equal(1, successfulBookings); // Ticket only booked by one user in concurrent requests.
          
        }

        [Fact]
        public async Task BookingConcurrency_ShouldOnlyAllowOneSuccessfulBookingUsingLock()
        {
            var ticketId = 9;
            var tasks = new List<Task>();
            var successfulBookings = 0;
            string userId = string.Empty;
            List<ResponseModel> Result = new List<ResponseModel>();
            // Act
            for (int i = 0; i < 10; i++)
            {

                tasks.Add(Task.Run(async () =>
                {
                    userId = Guid.NewGuid().ToString();
                    using (var scopeFactory = _scopeFactory.CreateScope())
                    {
                        var _ticketService = scopeFactory.ServiceProvider.GetRequiredService<ITicketService>();
                        var (success, bookedTicket) = await _ticketService.TryBookTicketUsingLockAsync(ticketId, userId);
                        Result.Add(new ResponseModel
                        {
                            Success = success,
                            Ticket = bookedTicket,
                            UserId = userId
                        });
                        if (success)
                        {
                            successfulBookings++;
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.All(tasks, task => Assert.True(task.IsCompletedSuccessfully));
            Assert.Equal(1, successfulBookings); // Ticket only booked by one user in concurrent requests.
            var trueValues = Result.Where(x => x.Success);
            // Assert that there is exactly one true value
            Assert.Single(trueValues);

        }
    }

    public sealed class ResponseModel
    {
        public bool Success { get; set; }
        public Ticket? Ticket { get; set; }
        public string UserId { get; set; }
    }
}

