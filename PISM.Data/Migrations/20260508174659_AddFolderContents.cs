using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PISM.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderContents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FolderContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FolderPath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderContents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FolderContents_FileHash",
                table: "FolderContents",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_FolderContents_FileHash_FolderPath",
                table: "FolderContents",
                columns: new[] { "FileHash", "FolderPath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FolderContents_FolderPath",
                table: "FolderContents",
                column: "FolderPath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FolderContents");
        }
    }
}
