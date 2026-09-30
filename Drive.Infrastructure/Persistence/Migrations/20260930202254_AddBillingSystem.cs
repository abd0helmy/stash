using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Drive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BillingInterval = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StorageLimitBytes = table.Column<long>(type: "bigint", nullable: false),
                    MonthlyRequestLimit = table.Column<int>(type: "integer", nullable: false),
                    MaxFileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StorageUsedBytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    ApiRequests = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    BandwidthUsedBytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelAtPeriodEnd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CanceledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "Id", "BillingInterval", "CreatedAt", "Currency", "IsActive", "MaxFileSizeBytes", "MonthlyRequestLimit", "Name", "Price", "StorageLimitBytes", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "month", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", true, 104857600L, 10000, "Free", 0.00m, 5368709120L, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "month", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", true, 2147483648L, 100000, "Pro", 4.99m, 107374182400L, null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "month", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", true, 10737418240L, 1000000, "Business", 14.99m, 1099511627776L, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Name",
                table: "Plans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions",
                column: "UserId",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId_Status",
                table: "Subscriptions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Usages_UserId_PeriodStart",
                table: "Usages",
                columns: new[] { "UserId", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_Usages_UserId_PeriodStart_PeriodEnd",
                table: "Usages",
                columns: new[] { "UserId", "PeriodStart", "PeriodEnd" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Usages");

            migrationBuilder.DropTable(
                name: "Plans");
        }
    }
}
