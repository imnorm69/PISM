using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PISM.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeletedHashes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DateDeleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedHashes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImageFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    OriginalFolder = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DateScanned = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsEncrypted = table.Column<bool>(type: "boolean", nullable: false),
                    EncryptedFilePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    CropX = table.Column<int>(type: "integer", nullable: true),
                    CropY = table.Column<int>(type: "integer", nullable: true),
                    CropWidth = table.Column<int>(type: "integer", nullable: true),
                    CropHeight = table.Column<int>(type: "integer", nullable: true),
                    RotationDegrees = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsDuplicate = table.Column<bool>(type: "boolean", nullable: false),
                    DuplicateOfId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageFiles_ImageFiles_DuplicateOfId",
                        column: x => x.DuplicateOfId,
                        principalTable: "ImageFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ScanJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FolderPath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    DateStarted = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateCompleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TotalFiles = table.Column<int>(type: "integer", nullable: false),
                    ProcessedFiles = table.Column<int>(type: "integer", nullable: false),
                    NewFiles = table.Column<int>(type: "integer", nullable: false),
                    DuplicatesFound = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImageTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageTags_ImageFiles_ImageFileId",
                        column: x => x.ImageFileId,
                        principalTable: "ImageFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeletedHashes_Hash",
                table: "DeletedHashes",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_DuplicateOfId",
                table: "ImageFiles",
                column: "DuplicateOfId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_Hash",
                table: "ImageFiles",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_Status",
                table: "ImageFiles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ImageTags_ImageFileId",
                table: "ImageTags",
                column: "ImageFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageTags_ImageFileId_Tag",
                table: "ImageTags",
                columns: new[] { "ImageFileId", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageTags_Tag",
                table: "ImageTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_Status",
                table: "ScanJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeletedHashes");

            migrationBuilder.DropTable(
                name: "ImageTags");

            migrationBuilder.DropTable(
                name: "ScanJobs");

            migrationBuilder.DropTable(
                name: "ImageFiles");
        }
    }
}
