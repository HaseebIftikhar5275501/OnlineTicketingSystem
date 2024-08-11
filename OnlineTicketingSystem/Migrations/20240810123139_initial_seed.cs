using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

#nullable disable

namespace OnlineTicketingSystem.Migrations
{
    /// <inheritdoc />
    public partial class initial_seed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            for (int i = 1; i <= 20; i++)
            {
                migrationBuilder.InsertData(
                    table: "Tickets",
                    columns: new[] { "Name", "UserId", "IsBooked" },
                    values: new object[] { $"Seat {i}", "", false }
                );
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
           
        }
    }
}
