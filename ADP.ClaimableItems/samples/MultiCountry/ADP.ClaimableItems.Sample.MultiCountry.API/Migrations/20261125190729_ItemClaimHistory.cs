using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.ClaimableItems.Sample.MultiCountry.API.Migrations
{
    /// <inheritdoc />
    public partial class ItemClaimHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemClaimStatusHistory",
                schema: "ClaimableItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorUserID = table.Column<long>(type: "bigint", nullable: true),
                    ActorUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActorRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimID = table.Column<long>(type: "bigint", nullable: false),
                    DistributorOnly = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemClaimStatusHistory", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ItemClaimStatusHistory_ItemClaim_ClaimID",
                        column: x => x.ClaimID,
                        principalSchema: "ClaimableItems",
                        principalTable: "ItemClaim",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemClaimStatusHistory_ClaimID_Timestamp",
                schema: "ClaimableItems",
                table: "ItemClaimStatusHistory",
                columns: new[] { "ClaimID", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemClaimStatusHistory",
                schema: "ClaimableItems");
        }
    }
}
