using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConstructionProjectTracker.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeDeadlineHistoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutomatic",
                table: "TaskDeadlineHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NewStartDate",
                table: "TaskDeadlineHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreviousStartDate",
                table: "TaskDeadlineHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutomatic",
                table: "ProjectDeadlineHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutomatic",
                table: "TaskDeadlineHistories");

            migrationBuilder.DropColumn(
                name: "NewStartDate",
                table: "TaskDeadlineHistories");

            migrationBuilder.DropColumn(
                name: "PreviousStartDate",
                table: "TaskDeadlineHistories");

            migrationBuilder.DropColumn(
                name: "IsAutomatic",
                table: "ProjectDeadlineHistories");
        }
    }
}
