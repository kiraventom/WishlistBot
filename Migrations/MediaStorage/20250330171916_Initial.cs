﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.MediaStorage
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoredMedia",
                columns: table => new
                {
                    MediaItemId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileId = table.Column<string>(type: "TEXT", nullable: true),
                    MessageId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredMedia", x => x.MediaItemId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoredMedia");
        }
    }
}
