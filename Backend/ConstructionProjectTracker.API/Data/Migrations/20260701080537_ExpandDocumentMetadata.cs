using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConstructionProjectTracker.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandDocumentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FileType",
                table: "Documents",
                newName: "ContentType");

            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "Documents",
                newName: "RelativeFilePath");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "Documents",
                newName: "StoredFileName");

            migrationBuilder.AddColumn<string>(
                name: "Extension",
                table: "Documents",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "Documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "Documents",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Extension",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "StoredFileName",
                table: "Documents",
                newName: "FileName");

            migrationBuilder.RenameColumn(
                name: "RelativeFilePath",
                table: "Documents",
                newName: "FilePath");

            migrationBuilder.RenameColumn(
                name: "ContentType",
                table: "Documents",
                newName: "FileType");
        }
    }
}
