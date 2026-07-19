using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConstructionProjectTracker.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletionApprovalHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskCompletionApprovalHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskCompletionReportId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCompletionApprovalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskCompletionApprovalHistories_TaskCompletionReports_TaskCompletionReportId",
                        column: x => x.TaskCompletionReportId,
                        principalTable: "TaskCompletionReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskCompletionApprovalHistories_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskCompletionApprovalHistories_ReviewedByUserId",
                table: "TaskCompletionApprovalHistories",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCompletionApprovalHistories_TaskCompletionReportId_ReviewedAt",
                table: "TaskCompletionApprovalHistories",
                columns: new[] { "TaskCompletionReportId", "ReviewedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskCompletionApprovalHistories");
        }
    }
}
