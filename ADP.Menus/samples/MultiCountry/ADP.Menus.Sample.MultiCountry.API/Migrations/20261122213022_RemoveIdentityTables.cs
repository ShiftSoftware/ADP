using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Menus.Sample.MultiCountry.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIdentityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Apps",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "AuthenticationAuditEvents",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "AuthenticationOperations",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "AuthenticationPolicyStates",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "AuthThrottleBuckets",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "CompanyBranchBrands",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "CompanyBranchDepartments",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "CompanyBranchServices",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "CompanyCalendarBranches",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "DeviceAuthorizations",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "TeamBranches",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "TeamUsers",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "UserAccessTrees",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "UserLogs",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "UserProviderLinks",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "UserSecurityStates",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Brands",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Departments",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Services",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "CompanyCalendars",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Teams",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "AccessTrees",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "CompanyBranches",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Cities",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Companies",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Regions",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Countries",
                schema: "ShiftIdentity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ShiftIdentity");

            migrationBuilder.CreateTable(
                name: "AccessTrees",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Tree = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessTrees", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Apps",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AppSecret = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    RedirectUri = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apps", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationAuditEvents",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OperationID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: false),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    VerificationReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationAuditEvents", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationPolicyStates",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false),
                    MfaEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MfaMandatory = table.Column<bool>(type: "bit", nullable: false),
                    RequireVerifiedEmail = table.Column<bool>(type: "bit", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TotpDigits = table.Column<int>(type: "int", nullable: false),
                    TotpPeriodSeconds = table.Column<int>(type: "int", nullable: false),
                    TotpWindowFuture = table.Column<int>(type: "int", nullable: false),
                    TotpWindowPast = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationPolicyStates", x => x.ID);
                    table.CheckConstraint("CK_AuthenticationPolicyState", "[ID] = 1 AND [Revision] >= 1");
                    table.CheckConstraint("CK_AuthenticationPolicyState_Totp", "[TotpDigits] BETWEEN 6 AND 8 AND [TotpPeriodSeconds] BETWEEN 1 AND 300 AND [TotpWindowPast] BETWEEN 0 AND 2 AND [TotpWindowFuture] BETWEEN 0 AND 2");
                });

            migrationBuilder.CreateTable(
                name: "AuthThrottleBuckets",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    WindowStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthThrottleBuckets", x => x.Key);
                    table.CheckConstraint("CK_AuthThrottleBucket", "[Count] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrandID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID"),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentCompanyID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID"),
                    CompanyType = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    CustomFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    HQAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HQEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HQPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsProtected = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LegalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Logo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Companies_Companies_ParentCompanyID",
                        column: x => x.ParentCompanyID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Companies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyCalendars",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntryType = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ShiftGroups = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WeekendGroups = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyCalendars", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallingCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CountryID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID"),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    Flag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsProtected = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TeamID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Teams_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Companies",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CompanyCalendarBranches",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCalendarID = table.Column<long>(type: "bigint", nullable: false),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyCalendarBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyCalendarBranches_CompanyCalendars_CompanyCalendarID",
                        column: x => x.CompanyCalendarID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "CompanyCalendars",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Regions",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryID = table.Column<long>(type: "bigint", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    Flag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsProtected = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegionID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID"),
                    ShortCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Regions_Countries_CountryID",
                        column: x => x.CountryID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Countries",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegionID = table.Column<long>(type: "bigint", nullable: true),
                    CityID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID"),
                    CountryID = table.Column<long>(type: "bigint", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsProtected = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Cities_Regions_RegionID",
                        column: x => x.RegionID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Regions",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CompanyBranches",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    RegionID = table.Column<long>(type: "bigint", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID"),
                    CountryID = table.Column<long>(type: "bigint", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    CustomFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Emails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsProtected = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    Latitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Longitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MobilePhotos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phones = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Photos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishTargets = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkingDays = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkingHours = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyBranches", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CompanyBranches_Cities_CityID",
                        column: x => x.CityID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Cities",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CompanyBranches_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Companies",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CompanyBranches_Regions_RegionID",
                        column: x => x.RegionID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Regions",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CompanyBranchBrands",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrandID = table.Column<long>(type: "bigint", nullable: false),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyBranchBrands", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CompanyBranchBrands_Brands_BrandID",
                        column: x => x.BrandID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Brands",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyBranchBrands_CompanyBranches_CompanyBranchID",
                        column: x => x.CompanyBranchID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "CompanyBranches",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyBranchDepartments",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyBranchDepartments", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CompanyBranchDepartments_CompanyBranches_CompanyBranchID",
                        column: x => x.CompanyBranchID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "CompanyBranches",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyBranchDepartments_Departments_DepartmentID",
                        column: x => x.DepartmentID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Departments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyBranchServices",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: false),
                    ServiceID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyBranchServices", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CompanyBranchServices_CompanyBranches_CompanyBranchID",
                        column: x => x.CompanyBranchID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "CompanyBranches",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyBranchServices_Services_ServiceID",
                        column: x => x.ServiceID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Services",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamBranches",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: false),
                    TeamID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamBranches", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TeamBranches_CompanyBranches_CompanyBranchID",
                        column: x => x.CompanyBranchID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "CompanyBranches",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamBranches_Teams_TeamID",
                        column: x => x.TeamID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Teams",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    CountryID = table.Column<long>(type: "bigint", nullable: true),
                    RegionID = table.Column<long>(type: "bigint", nullable: true),
                    AccessTree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllowDeviceSignIn = table.Column<bool>(type: "bit", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IntegrationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsProtected = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSeen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockDownUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LoginAttempts = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PhoneVerified = table.Column<bool>(type: "bit", nullable: false),
                    RequireChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    Salt = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Signature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotpSecret = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    VerificationSASToken = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Users_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Companies",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Users_CompanyBranches_CompanyBranchID",
                        column: x => x.CompanyBranchID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "CompanyBranches",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Users_Countries_CountryID",
                        column: x => x.CountryID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Countries",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Users_Regions_RegionID",
                        column: x => x.RegionID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Regions",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationOperations",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppBinding = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Audience = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ClientID = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CodeChallenge = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ContactRevision = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    External = table.Column<bool>(type: "bit", nullable: false),
                    FactorGeneration = table.Column<long>(type: "bigint", nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    HandleDigest = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    MfaProvenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OutstandingLinkSlot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    OutstandingRecoveryUserID = table.Column<long>(type: "bigint", nullable: true),
                    ParentID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PasswordChangeOrigin = table.Column<int>(type: "int", nullable: true),
                    PasswordProvenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PendingPasswordHash = table.Column<byte[]>(type: "varbinary(256)", maxLength: 256, nullable: true),
                    PendingPasswordSalt = table.Column<byte[]>(type: "varbinary(128)", maxLength: 128, nullable: true),
                    PolicyRevision = table.Column<long>(type: "bigint", nullable: false),
                    ProtectedPendingTotpSecret = table.Column<byte[]>(type: "varbinary(1024)", maxLength: 1024, nullable: true),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    RecoveryCodeDigest = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: false),
                    SessionAuthenticatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SessionLegacyCompatibilityExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SessionMfaSatisfied = table.Column<bool>(type: "bit", nullable: true),
                    SessionProvider = table.Column<int>(type: "int", nullable: true),
                    SessionTenant = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    SourceSessionDigest = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: true),
                    State = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationOperations", x => x.ID);
                    table.CheckConstraint("CK_AuthenticationOperation_App", "([State] <> 11 OR ([Purpose] = 10 AND [External] = 1 AND [AppBinding] IS NOT NULL AND [SessionAuthenticatedAt] IS NOT NULL AND [SessionMfaSatisfied] IS NOT NULL)) AND ([Purpose] <> 10 OR [State] IN (2,3,6,9,11))");
                    table.CheckConstraint("CK_AuthenticationOperation_Factor", "[ProtectedPendingTotpSecret] IS NULL OR ([Purpose] IN (3,4,5,6) AND [State] = 7)");
                    table.CheckConstraint("CK_AuthenticationOperation_LegacyMfa", "[Purpose] <> 12 OR ([State] IN (1,2,3,6) AND [External] = 0 AND DATALENGTH([HandleDigest]) = 32 AND [CodeChallenge] = '' AND ([State] = 1 OR [CompletedAt] IS NOT NULL))");
                    table.CheckConstraint("CK_AuthenticationOperation_LegacyRefresh", "[Purpose] <> 11 OR ([State] = 2 AND [External] = 0 AND DATALENGTH([HandleDigest]) = 32 AND [CompletedAt] IS NOT NULL)");
                    table.CheckConstraint("CK_AuthenticationOperation_Link", "([State] <> 10 OR [Purpose] IN (7,8,9)) AND ([OutstandingLinkSlot] IS NULL OR ([Purpose] IN (7,8,9) AND [State] = 10))");
                    table.CheckConstraint("CK_AuthenticationOperation_Password", "([PasswordChangeOrigin] IS NULL OR [PasswordChangeOrigin] IN (1,2)) AND (([PendingPasswordHash] IS NULL AND [PendingPasswordSalt] IS NULL) OR ([Purpose] = 4 AND [State] IN (1,7) AND [PendingPasswordHash] IS NOT NULL AND [PendingPasswordSalt] IS NOT NULL))");
                    table.CheckConstraint("CK_AuthenticationOperation_Provider", "([SessionProvider] IS NULL OR ([SessionProvider] IN (1, 2) AND [Purpose] IN (1,3,10,13,14))) AND ([State] <> 12 OR ([Purpose] = 14 AND [SessionProvider] IS NOT NULL) OR [Purpose] = 13) AND ([Purpose] <> 14 OR [State] IN (2,3,6,12)) AND ([SessionTenant] IS NULL OR [SessionProvider] = 1)");
                    table.CheckConstraint("CK_AuthenticationOperation_Recovery", "([RecoveryCodeDigest] IS NULL OR ([Purpose] = 6 AND [State] = 8 AND [ParentID] IS NULL)) AND ([OutstandingRecoveryUserID] IS NULL OR ([Purpose] = 6 AND [ParentID] IS NULL AND [OutstandingRecoveryUserID] = [UserID]))");
                    table.CheckConstraint("CK_AuthenticationOperation_SessionCompatibility", "[SessionLegacyCompatibilityExpiresAt] IS NULL OR ([Purpose] = 10 AND [State] = 11)");
                    table.CheckConstraint("CK_AuthenticationOperation_State", "[State] IN (1,2,3,4,5,6,7,8,9,10,11,12) AND [Purpose] IN (1,2,3,4,5,6,7,8,9,10,11,12,13,14)");
                    table.CheckConstraint("CK_AuthenticationOperation_Version", "[SecurityVersion] >= 1 AND [FactorGeneration] >= 1 AND [PolicyRevision] >= 1 AND [FailedAttempts] BETWEEN 0 AND 5 AND [ExpiresAt] > [CreatedAt]");
                    table.ForeignKey(
                        name: "FK_AuthenticationOperations_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceAuthorizations",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Audience = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AuthenticatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClientID = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeviceCodeDigest = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FactorGeneration = table.Column<long>(type: "bigint", nullable: true),
                    Interval = table.Column<int>(type: "int", nullable: false),
                    LastPolledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MfaSatisfied = table.Column<bool>(type: "bit", nullable: true),
                    PolicyRevision = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: true),
                    SessionProvider = table.Column<int>(type: "int", nullable: true),
                    State = table.Column<int>(type: "int", nullable: false),
                    UserCodeDigest = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: true),
                    UserID = table.Column<long>(type: "bigint", nullable: true)
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
                name: "TeamUsers",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamID = table.Column<long>(type: "bigint", nullable: false),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamUsers", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TeamUsers_Teams_TeamID",
                        column: x => x.TeamID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Teams",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamUsers_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAccessTrees",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessTreeID = table.Column<long>(type: "bigint", nullable: false),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccessTrees", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UserAccessTrees_AccessTrees_AccessTreeID",
                        column: x => x.AccessTreeID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "AccessTrees",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAccessTrees_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserLogs",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSeen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UserLogs_Users_UserID",
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
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EmailLookupKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Latin1_General_100_BIN2"),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ObjectID = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Latin1_General_100_BIN2"),
                    PersonalAccount = table.Column<bool>(type: "bit", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    TenantID = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    UserID = table.Column<long>(type: "bigint", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "UserSecurityStates",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    ContactRevision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    DeliveryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DeliveryWindowStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EmailLookupKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, collation: "Latin1_General_100_BIN2"),
                    FactorGeneration = table.Column<long>(type: "bigint", nullable: false),
                    FailedProofs = table.Column<int>(type: "int", nullable: false),
                    FailureWindowStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAcceptedTotpStep = table.Column<long>(type: "bigint", nullable: true),
                    LastDeliveryAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LocalMfaRecoveryRequired = table.Column<bool>(type: "bit", nullable: false),
                    MfaRecoveryOperationID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProtectedTotpSecret = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    RecoveryEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RecoveryEmailProvenance = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RecoveryEmailRevision = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: false),
                    TotpProtectionVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UsernameLookupKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, collation: "Latin1_General_100_BIN2")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSecurityStates", x => x.UserID);
                    table.CheckConstraint("CK_UserSecurityState_ContactDelivery", "[ContactRevision] >= 1 AND [DeliveryCount] >= 0 AND [RecoveryEmailProvenance] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_UserSecurityState_Lookup", "([UsernameLookupKey] IS NULL AND [EmailLookupKey] IS NULL) OR ([UsernameLookupKey] IS NOT NULL AND DATALENGTH([UsernameLookupKey]) > 0 AND ([EmailLookupKey] IS NULL OR DATALENGTH([EmailLookupKey]) > 0))");
                    table.CheckConstraint("CK_UserSecurityState_Version", "[SecurityVersion] >= 1 AND [FactorGeneration] >= 1 AND [FailedProofs] >= 0 AND [TotpProtectionVersion] IN (0,1)");
                    table.ForeignKey(
                        name: "FK_UserSecurityStates_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessTrees_Name",
                schema: "ShiftIdentity",
                table: "AccessTrees",
                column: "Name",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Apps_AppId",
                schema: "ShiftIdentity",
                table: "Apps",
                column: "AppId",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationAuditEvents_CreatedAt",
                schema: "ShiftIdentity",
                table: "AuthenticationAuditEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationOperations_ExpiresAt_State",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                columns: new[] { "ExpiresAt", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationOperations_HandleDigest",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                column: "HandleDigest",
                unique: true,
                filter: "[Purpose] IN (11,12)");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationOperations_OutstandingLinkSlot",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                column: "OutstandingLinkSlot",
                unique: true,
                filter: "[OutstandingLinkSlot] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationOperations_OutstandingRecoveryUserID",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                column: "OutstandingRecoveryUserID",
                unique: true,
                filter: "[OutstandingRecoveryUserID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationOperations_ParentID",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                column: "ParentID");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationOperations_UserID_Purpose_State",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                columns: new[] { "UserID", "Purpose", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthThrottleBuckets_WindowStart",
                schema: "ShiftIdentity",
                table: "AuthThrottleBuckets",
                column: "WindowStart");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_DisplayOrder",
                schema: "ShiftIdentity",
                table: "Cities",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_RegionID",
                schema: "ShiftIdentity",
                table: "Cities",
                column: "RegionID");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_DisplayOrder",
                schema: "ShiftIdentity",
                table: "Companies",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ParentCompanyID",
                schema: "ShiftIdentity",
                table: "Companies",
                column: "ParentCompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranchBrands_BrandID",
                schema: "ShiftIdentity",
                table: "CompanyBranchBrands",
                column: "BrandID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranchBrands_CompanyBranchID",
                schema: "ShiftIdentity",
                table: "CompanyBranchBrands",
                column: "CompanyBranchID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranchDepartments_CompanyBranchID",
                schema: "ShiftIdentity",
                table: "CompanyBranchDepartments",
                column: "CompanyBranchID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranchDepartments_DepartmentID",
                schema: "ShiftIdentity",
                table: "CompanyBranchDepartments",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranches_CityID",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                column: "CityID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranches_CompanyID",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranches_DisplayOrder",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranches_RegionID",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                column: "RegionID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranchServices_CompanyBranchID",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                column: "CompanyBranchID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranchServices_ServiceID",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCalendarBranches_CompanyCalendarID",
                schema: "ShiftIdentity",
                table: "CompanyCalendarBranches",
                column: "CompanyCalendarID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCalendars_StartDate_EndDate_CompanyID_IsDeleted",
                schema: "ShiftIdentity",
                table: "CompanyCalendars",
                columns: new[] { "StartDate", "EndDate", "CompanyID", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Countries_DisplayOrder",
                schema: "ShiftIdentity",
                table: "Countries",
                column: "DisplayOrder");

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
                name: "IX_Regions_CountryID",
                schema: "ShiftIdentity",
                table: "Regions",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_Regions_DisplayOrder",
                schema: "ShiftIdentity",
                table: "Regions",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_TeamBranches_CompanyBranchID",
                schema: "ShiftIdentity",
                table: "TeamBranches",
                column: "CompanyBranchID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamBranches_TeamID",
                schema: "ShiftIdentity",
                table: "TeamBranches",
                column: "TeamID");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CompanyID",
                schema: "ShiftIdentity",
                table: "Teams",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamUsers_TeamID",
                schema: "ShiftIdentity",
                table: "TeamUsers",
                column: "TeamID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamUsers_UserID",
                schema: "ShiftIdentity",
                table: "TeamUsers",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessTrees_AccessTreeID",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                column: "AccessTreeID");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessTrees_UserID",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogs_UserID",
                schema: "ShiftIdentity",
                table: "UserLogs",
                column: "UserID",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyBranchID",
                schema: "ShiftIdentity",
                table: "Users",
                column: "CompanyBranchID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyID",
                schema: "ShiftIdentity",
                table: "Users",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CountryID",
                schema: "ShiftIdentity",
                table: "Users",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "ShiftIdentity",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "IsDeleted = 0 AND Email is not null");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IntegrationId",
                schema: "ShiftIdentity",
                table: "Users",
                column: "IntegrationId",
                unique: true,
                filter: "IsDeleted = 0 AND IntegrationId is not null");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                schema: "ShiftIdentity",
                table: "Users",
                column: "Phone",
                unique: true,
                filter: "IsDeleted = 0 AND Phone is not null");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RegionID",
                schema: "ShiftIdentity",
                table: "Users",
                column: "RegionID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                schema: "ShiftIdentity",
                table: "Users",
                column: "Username",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurityStates_EmailLookupKey",
                schema: "ShiftIdentity",
                table: "UserSecurityStates",
                column: "EmailLookupKey",
                unique: true,
                filter: "[EmailLookupKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurityStates_UsernameLookupKey",
                schema: "ShiftIdentity",
                table: "UserSecurityStates",
                column: "UsernameLookupKey",
                unique: true,
                filter: "[UsernameLookupKey] IS NOT NULL");
        }
    }
}
