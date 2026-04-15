using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Menus.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ShiftIdentity");

            migrationBuilder.EnsureSchema(
                name: "Menu");

            migrationBuilder.CreateTable(
                name: "AccessTrees",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Tree = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AppId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AppSecret = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RedirectUri = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apps", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "BrandMapping",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrandAbbreviation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrandID = table.Column<long>(type: "bigint", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandMapping", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrandID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID"),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyType = table.Column<int>(type: "int", nullable: false),
                    Logo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HQPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HQEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HQAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuiltIn = table.Column<bool>(type: "bit", nullable: false),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentCompanyID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntryType = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    LastReplicationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShiftGroups = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WeekendGroups = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CallingCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BuiltIn = table.Column<bool>(type: "bit", nullable: false),
                    CountryID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID"),
                    Flag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "DeletedRowLogs",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowID = table.Column<long>(type: "bigint", nullable: false),
                    PartitionKeyLevelOneValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartitionKeyLevelOneType = table.Column<int>(type: "int", nullable: false),
                    PartitionKeyLevelTwoValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartitionKeyLevelTwoType = table.Column<int>(type: "int", nullable: false),
                    PartitionKeyLevelThreeValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartitionKeyLevelThreeType = table.Column<int>(type: "int", nullable: false),
                    ContainerName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastReplicationDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedRowLogs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LabourRateMapping",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LabourRate = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrandID = table.Column<long>(type: "bigint", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabourRateMapping", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "MenuVersion",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    VersionDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuVersion", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceIntervalGroup",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LabourCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LabourDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceIntervalGroup", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "StandaloneReplacementItemGroup",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MenuCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LabourCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandaloneReplacementItemGroup", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TodoItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDone = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItems", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "VehicleModel",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrandID = table.Column<long>(type: "bigint", nullable: true),
                    LabourRate = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModel", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TeamID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID"),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuiltIn = table.Column<bool>(type: "bit", nullable: false),
                    CountryID = table.Column<long>(type: "bigint", nullable: true),
                    RegionID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID"),
                    Flag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                name: "ServiceInterval",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ValueInMeter = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServiceIntervalGroupID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceInterval", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ServiceInterval_ServiceIntervalGroup_ServiceIntervalGroupID",
                        column: x => x.ServiceIntervalGroupID,
                        principalSchema: "Menu",
                        principalTable: "ServiceIntervalGroup",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReplacementItem",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FriendlyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    AllowMultiplePartNumbers = table.Column<bool>(type: "bit", nullable: false),
                    DefaultPartPriceMarginPercentage = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    StandaloneOperationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StandaloneLabourCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StandaloneReplacementItemGroupID = table.Column<long>(type: "bigint", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplacementItem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ReplacementItem_StandaloneReplacementItemGroup_StandaloneReplacementItemGroupID",
                        column: x => x.StandaloneReplacementItemGroupID,
                        principalSchema: "Menu",
                        principalTable: "StandaloneReplacementItemGroup",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Menu",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BasicModelCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BrandID = table.Column<long>(type: "bigint", nullable: true),
                    VehicleModelID = table.Column<long>(type: "bigint", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menu", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Menu_VehicleModel_VehicleModelID",
                        column: x => x.VehicleModelID,
                        principalSchema: "Menu",
                        principalTable: "VehicleModel",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleModelLabourDetails",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleModelID = table.Column<long>(type: "bigint", nullable: false),
                    ServiceIntervalGroupID = table.Column<long>(type: "bigint", nullable: false),
                    AllowedTime = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Consumable = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModelLabourDetails", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VehicleModelLabourDetails_ServiceIntervalGroup_ServiceIntervalGroupID",
                        column: x => x.ServiceIntervalGroupID,
                        principalSchema: "Menu",
                        principalTable: "ServiceIntervalGroup",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleModelLabourDetails_VehicleModel_VehicleModelID",
                        column: x => x.VehicleModelID,
                        principalSchema: "Menu",
                        principalTable: "VehicleModel",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleModelLabourRate",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleModelID = table.Column<long>(type: "bigint", nullable: false),
                    CountryID = table.Column<long>(type: "bigint", nullable: false),
                    LabourRate = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModelLabourRate", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VehicleModelLabourRate_VehicleModel_VehicleModelID",
                        column: x => x.VehicleModelID,
                        principalSchema: "Menu",
                        principalTable: "VehicleModel",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegionID = table.Column<long>(type: "bigint", nullable: true),
                    BuiltIn = table.Column<bool>(type: "bit", nullable: false),
                    CountryID = table.Column<long>(type: "bigint", nullable: true),
                    CityID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                name: "ReplacementItemServiceIntervalGroup",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReplacementItemID = table.Column<long>(type: "bigint", nullable: false),
                    ServiceIntervalGroupID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplacementItemServiceIntervalGroup", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ReplacementItemServiceIntervalGroup_ReplacementItem_ReplacementItemID",
                        column: x => x.ReplacementItemID,
                        principalSchema: "Menu",
                        principalTable: "ReplacementItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplacementItemServiceIntervalGroup_ServiceIntervalGroup_ServiceIntervalGroupID",
                        column: x => x.ServiceIntervalGroupID,
                        principalSchema: "Menu",
                        principalTable: "ServiceIntervalGroup",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReplacementItemVehicleModel",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReplacementItemID = table.Column<long>(type: "bigint", nullable: false),
                    VehicleModelID = table.Column<long>(type: "bigint", nullable: false),
                    StandaloneAllowedTime = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    DefaultPartPriceMarginPercentage = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplacementItemVehicleModel", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ReplacementItemVehicleModel_ReplacementItem_ReplacementItemID",
                        column: x => x.ReplacementItemID,
                        principalSchema: "Menu",
                        principalTable: "ReplacementItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplacementItemVehicleModel_VehicleModel_VehicleModelID",
                        column: x => x.VehicleModelID,
                        principalSchema: "Menu",
                        principalTable: "VehicleModel",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuVariant",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuID = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MenuPrefix = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MenuPostfix = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LabourRate = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    HasStandaloneItems = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuVariant", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MenuVariant_Menu_MenuID",
                        column: x => x.MenuID,
                        principalSchema: "Menu",
                        principalTable: "Menu",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyBranches",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phones = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Emails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Longitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Photos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MobilePhotos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkingHours = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkingDays = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntegrationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuiltIn = table.Column<bool>(type: "bit", nullable: false),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegionID = table.Column<long>(type: "bigint", nullable: true),
                    CityID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    CountryID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "ID"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishTargets = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                name: "ReplacementItemVehicleModelPart",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReplacementItemVehicleModelID = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    PartNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultPeriodicQuantity = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    DefaultStandaloneQuantity = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplacementItemVehicleModelPart", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ReplacementItemVehicleModelPart_ReplacementItemVehicleModel_ReplacementItemVehicleModelID",
                        column: x => x.ReplacementItemVehicleModelID,
                        principalSchema: "Menu",
                        principalTable: "ReplacementItemVehicleModel",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuItem",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuVariantID = table.Column<long>(type: "bigint", nullable: false),
                    ReplacementItemVehicleModelID = table.Column<long>(type: "bigint", nullable: true),
                    StandaloneAllowedTime = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MenuItem_MenuVariant_MenuVariantID",
                        column: x => x.MenuVariantID,
                        principalSchema: "Menu",
                        principalTable: "MenuVariant",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItem_ReplacementItemVehicleModel_ReplacementItemVehicleModelID",
                        column: x => x.ReplacementItemVehicleModelID,
                        principalSchema: "Menu",
                        principalTable: "ReplacementItemVehicleModel",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuLabourDetails",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuVariantID = table.Column<long>(type: "bigint", nullable: false),
                    ServiceIntervalGroupID = table.Column<long>(type: "bigint", nullable: false),
                    AllowedTime = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Consumable = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuLabourDetails", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MenuLabourDetails_MenuVariant_MenuVariantID",
                        column: x => x.MenuVariantID,
                        principalSchema: "Menu",
                        principalTable: "MenuVariant",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuLabourDetails_ServiceIntervalGroup_ServiceIntervalGroupID",
                        column: x => x.ServiceIntervalGroupID,
                        principalSchema: "Menu",
                        principalTable: "ServiceIntervalGroup",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuPeriodicAvailability",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuVariantID = table.Column<long>(type: "bigint", nullable: false),
                    ServiceIntervalID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuPeriodicAvailability", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MenuPeriodicAvailability_MenuVariant_MenuVariantID",
                        column: x => x.MenuVariantID,
                        principalSchema: "Menu",
                        principalTable: "MenuVariant",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuPeriodicAvailability_ServiceInterval_ServiceIntervalID",
                        column: x => x.ServiceIntervalID,
                        principalSchema: "Menu",
                        principalTable: "ServiceInterval",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuVariantLabourRate",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuVariantID = table.Column<long>(type: "bigint", nullable: false),
                    CountryID = table.Column<long>(type: "bigint", nullable: false),
                    LabourRate = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuVariantLabourRate", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MenuVariantLabourRate_MenuVariant_MenuVariantID",
                        column: x => x.MenuVariantID,
                        principalSchema: "Menu",
                        principalTable: "MenuVariant",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyBranchBrands",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: false),
                    BrandID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                    Username = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IntegrationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Salt = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    LoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockDownUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSuperAdmin = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    BuiltIn = table.Column<bool>(type: "bit", nullable: false),
                    AccessTree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequireChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    VerificationSASToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PhoneVerified = table.Column<bool>(type: "bit", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSeen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CountryID = table.Column<long>(type: "bigint", nullable: true),
                    RegionID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: true),
                    Signature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                name: "MenuItemPart",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuItemID = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    PartNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PeriodicQuantity = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    StandaloneQuantity = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemPart", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MenuItemPart_MenuItem_MenuItemID",
                        column: x => x.MenuItemID,
                        principalSchema: "Menu",
                        principalTable: "MenuItem",
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
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    TeamID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    AccessTreeID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                    LastSeen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                name: "MenuItemPartCountryPrice",
                schema: "Menu",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuItemPartID = table.Column<long>(type: "bigint", nullable: false),
                    CountryID = table.Column<long>(type: "bigint", nullable: false),
                    PartPrice = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    PartPriceMarginPercentage = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    PartFinalPrice = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemPartCountryPrice", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MenuItemPartCountryPrice_MenuItemPart_MenuItemPartID",
                        column: x => x.MenuItemPartID,
                        principalSchema: "Menu",
                        principalTable: "MenuItemPart",
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
                name: "IX_BrandMapping_BrandID",
                schema: "Menu",
                table: "BrandMapping",
                column: "BrandID",
                unique: true,
                filter: "IsDeleted = 0");

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
                name: "IX_DeletedRowLogs_ContainerName_LastReplicationDate",
                table: "DeletedRowLogs",
                columns: new[] { "ContainerName", "LastReplicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LabourRateMapping_BrandID_LabourRate",
                schema: "Menu",
                table: "LabourRateMapping",
                columns: new[] { "BrandID", "LabourRate" },
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_BasicModelCode",
                schema: "Menu",
                table: "Menu",
                column: "BasicModelCode",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_VehicleModelID",
                schema: "Menu",
                table: "Menu",
                column: "VehicleModelID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_MenuVariantID_ReplacementItemVehicleModelID",
                schema: "Menu",
                table: "MenuItem",
                columns: new[] { "MenuVariantID", "ReplacementItemVehicleModelID" },
                unique: true,
                filter: "IsDeleted = 0 AND ReplacementItemVehicleModelID IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_ReplacementItemVehicleModelID",
                schema: "Menu",
                table: "MenuItem",
                column: "ReplacementItemVehicleModelID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPart_MenuItemID_SortOrder",
                schema: "Menu",
                table: "MenuItemPart",
                columns: new[] { "MenuItemID", "SortOrder" },
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPartCountryPrice_MenuItemPartID_CountryID",
                schema: "Menu",
                table: "MenuItemPartCountryPrice",
                columns: new[] { "MenuItemPartID", "CountryID" },
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MenuLabourDetails_MenuVariantID_ServiceIntervalGroupID",
                schema: "Menu",
                table: "MenuLabourDetails",
                columns: new[] { "MenuVariantID", "ServiceIntervalGroupID" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuLabourDetails_ServiceIntervalGroupID",
                schema: "Menu",
                table: "MenuLabourDetails",
                column: "ServiceIntervalGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuPeriodicAvailability_MenuVariantID_ServiceIntervalID",
                schema: "Menu",
                table: "MenuPeriodicAvailability",
                columns: new[] { "MenuVariantID", "ServiceIntervalID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuPeriodicAvailability_ServiceIntervalID",
                schema: "Menu",
                table: "MenuPeriodicAvailability",
                column: "ServiceIntervalID");

            migrationBuilder.CreateIndex(
                name: "IX_MenuVariant_MenuID_MenuPrefix_MenuPostfix",
                schema: "Menu",
                table: "MenuVariant",
                columns: new[] { "MenuID", "MenuPrefix", "MenuPostfix" },
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MenuVariant_MenuID_Name",
                schema: "Menu",
                table: "MenuVariant",
                columns: new[] { "MenuID", "Name" },
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MenuVariantLabourRate_MenuVariantID_CountryID",
                schema: "Menu",
                table: "MenuVariantLabourRate",
                columns: new[] { "MenuVariantID", "CountryID" },
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MenuVersion_Version",
                schema: "Menu",
                table: "MenuVersion",
                column: "Version",
                unique: true);

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
                name: "IX_ReplacementItem_Name",
                schema: "Menu",
                table: "ReplacementItem",
                column: "Name",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementItem_StandaloneReplacementItemGroupID",
                schema: "Menu",
                table: "ReplacementItem",
                column: "StandaloneReplacementItemGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementItemServiceIntervalGroup_ReplacementItemID_ServiceIntervalGroupID",
                schema: "Menu",
                table: "ReplacementItemServiceIntervalGroup",
                columns: new[] { "ReplacementItemID", "ServiceIntervalGroupID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementItemServiceIntervalGroup_ServiceIntervalGroupID",
                schema: "Menu",
                table: "ReplacementItemServiceIntervalGroup",
                column: "ServiceIntervalGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementItemVehicleModel_ReplacementItemID",
                schema: "Menu",
                table: "ReplacementItemVehicleModel",
                column: "ReplacementItemID");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementItemVehicleModel_VehicleModelID",
                schema: "Menu",
                table: "ReplacementItemVehicleModel",
                column: "VehicleModelID");

            migrationBuilder.CreateIndex(
                name: "IX_ReplacementItemVehicleModelPart_ReplacementItemVehicleModelID_SortOrder",
                schema: "Menu",
                table: "ReplacementItemVehicleModelPart",
                columns: new[] { "ReplacementItemVehicleModelID", "SortOrder" },
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInterval_Code",
                schema: "Menu",
                table: "ServiceInterval",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInterval_FullName",
                schema: "Menu",
                table: "ServiceInterval",
                column: "FullName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInterval_ServiceIntervalGroupID",
                schema: "Menu",
                table: "ServiceInterval",
                column: "ServiceIntervalGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInterval_ValueInMeter",
                schema: "Menu",
                table: "ServiceInterval",
                column: "ValueInMeter",
                unique: true);

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
                name: "IX_VehicleModelLabourDetails_ServiceIntervalGroupID",
                schema: "Menu",
                table: "VehicleModelLabourDetails",
                column: "ServiceIntervalGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModelLabourDetails_VehicleModelID_ServiceIntervalGroupID",
                schema: "Menu",
                table: "VehicleModelLabourDetails",
                columns: new[] { "VehicleModelID", "ServiceIntervalGroupID" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModelLabourRate_VehicleModelID_CountryID",
                schema: "Menu",
                table: "VehicleModelLabourRate",
                columns: new[] { "VehicleModelID", "CountryID" },
                unique: true,
                filter: "IsDeleted = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Apps",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "BrandMapping",
                schema: "Menu");

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
                name: "DeletedRowLogs");

            migrationBuilder.DropTable(
                name: "LabourRateMapping",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "MenuItemPartCountryPrice",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "MenuLabourDetails",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "MenuPeriodicAvailability",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "MenuVariantLabourRate",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "MenuVersion",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "ReplacementItemServiceIntervalGroup",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "ReplacementItemVehicleModelPart",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "TeamBranches",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "TeamUsers",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "TodoItems");

            migrationBuilder.DropTable(
                name: "UserAccessTrees",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "UserLogs",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "VehicleModelLabourDetails",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "VehicleModelLabourRate",
                schema: "Menu");

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
                name: "MenuItemPart",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "ServiceInterval",
                schema: "Menu");

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
                name: "MenuItem",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "ServiceIntervalGroup",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "CompanyBranches",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "MenuVariant",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "ReplacementItemVehicleModel",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "Cities",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Companies",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Menu",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "ReplacementItem",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "Regions",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "VehicleModel",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "StandaloneReplacementItemGroup",
                schema: "Menu");

            migrationBuilder.DropTable(
                name: "Countries",
                schema: "ShiftIdentity");
        }
    }
}
