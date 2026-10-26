using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Menus.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowDeviceSignInToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AuthenticationOperation_State",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations");

            migrationBuilder.AddColumn<bool>(
                name: "AllowDeviceSignIn",
                schema: "ShiftIdentity",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "TeamUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "TeamUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionProvider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "AccessTrees",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "AccessTrees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeviceAuthorizations",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceCodeDigest = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: true),
                    UserCodeDigest = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: true),
                    ClientID = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Audience = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<long>(type: "bigint", nullable: true),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: true),
                    PolicyRevision = table.Column<long>(type: "bigint", nullable: true),
                    FactorGeneration = table.Column<long>(type: "bigint", nullable: true),
                    MfaSatisfied = table.Column<bool>(type: "bit", nullable: true),
                    AuthenticatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SessionProvider = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastPolledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Interval = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceAuthorizations", x => x.ID);
                    table.CheckConstraint("CK_DeviceAuthorization_Account", "([State] = 1 AND [UserID] IS NULL AND [ApprovedAt] IS NULL) OR ([State] IN (2,4) AND [UserID] IS NOT NULL AND [SecurityVersion] >= 1 AND [PolicyRevision] >= 1 AND [FactorGeneration] >= 1 AND [MfaSatisfied] IS NOT NULL AND [AuthenticatedAt] IS NOT NULL AND [ApprovedAt] IS NOT NULL) OR [State] IN (3,5)");
                    table.CheckConstraint("CK_DeviceAuthorization_Codes", "([State] IN (1,2) AND DATALENGTH([DeviceCodeDigest]) = 32 AND DATALENGTH([UserCodeDigest]) = 32) OR ([State] IN (4,5) AND [DeviceCodeDigest] IS NULL AND [UserCodeDigest] IS NULL) OR ([State] = 3 AND ((DATALENGTH([DeviceCodeDigest]) = 32 AND DATALENGTH([UserCodeDigest]) = 32) OR ([DeviceCodeDigest] IS NULL AND [UserCodeDigest] IS NULL)))");
                    table.CheckConstraint("CK_DeviceAuthorization_State", "[State] IN (1,2,3,4,5) AND [ExpiresAt] > [CreatedAt] AND [Interval] BETWEEN 1 AND 3600 AND ([SessionProvider] IS NULL OR [SessionProvider] IN (1, 2))");
                    table.ForeignKey(
                        name: "FK_DeviceAuthorizations_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserProviderLinks",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    TenantID = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ObjectID = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Latin1_General_100_BIN2"),
                    EmailLookupKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PersonalAccount = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProviderLinks", x => x.ID);
                    table.CheckConstraint("CK_UserProviderLink", "[Provider] IN (1, 2) AND DATALENGTH([TenantID]) > 0 AND DATALENGTH([ObjectID]) > 0 AND DATALENGTH([EmailLookupKey]) > 0");
                    table.ForeignKey(
                        name: "FK_UserProviderLinks_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuthenticationOperation_Provider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                sql: "([SessionProvider] IS NULL OR ([SessionProvider] IN (1, 2) AND [Purpose] IN (1,3,10,14))) AND ([State] <> 12 OR ([Purpose] = 14 AND [SessionProvider] IS NOT NULL)) AND ([Purpose] <> 14 OR [State] IN (2,3,6,12))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuthenticationOperation_State",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                sql: "[State] IN (1,2,3,4,5,6,7,8,9,10,11,12) AND [Purpose] IN (1,2,3,4,5,6,7,8,9,10,11,12,13,14)");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAuthorizations_ExpiresAt_State",
                schema: "ShiftIdentity",
                table: "DeviceAuthorizations",
                columns: new[] { "ExpiresAt", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAuthorizations_UserCodeDigest",
                schema: "ShiftIdentity",
                table: "DeviceAuthorizations",
                column: "UserCodeDigest",
                unique: true,
                filter: "[UserCodeDigest] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAuthorizations_UserID",
                schema: "ShiftIdentity",
                table: "DeviceAuthorizations",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserProviderLinks_Provider_TenantID_ObjectID",
                schema: "ShiftIdentity",
                table: "UserProviderLinks",
                columns: new[] { "Provider", "TenantID", "ObjectID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProviderLinks_UserID",
                schema: "ShiftIdentity",
                table: "UserProviderLinks",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceAuthorizations",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "UserProviderLinks",
                schema: "ShiftIdentity");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AuthenticationOperation_Provider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AuthenticationOperation_State",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations");

            migrationBuilder.DropColumn(
                name: "AllowDeviceSignIn",
                schema: "ShiftIdentity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "UserAccessTrees");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "TeamUsers");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "TeamUsers");

            migrationBuilder.DropColumn(
                name: "SessionProvider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "AccessTrees");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "AccessTrees");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuthenticationOperation_State",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                sql: "[State] IN (1,2,3,4,5,6,7,8,9,10,11) AND [Purpose] IN (1,2,3,4,5,6,7,8,9,10,11,12,13)");
        }
    }
}
