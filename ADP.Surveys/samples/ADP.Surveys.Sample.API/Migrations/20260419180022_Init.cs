using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Surveys.Sample.API.Migrations
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
                name: "Surveys");

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
                name: "BankQuestion",
                schema: "Surveys",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankEntryID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    QuestionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BiColumn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Locked = table.Column<bool>(type: "bit", nullable: false),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankQuestion", x => x.ID);
                    table.UniqueConstraint("AK_BankQuestion_BankEntryID", x => x.BankEntryID);
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
                name: "ScreenTemplate",
                schema: "Surveys",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TemplateJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScreenTemplate", x => x.ID);
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
                name: "Survey",
                schema: "Surveys",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DraftJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedVersionNumber = table.Column<int>(type: "int", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey", x => x.ID);
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
                name: "SurveyVersion",
                schema: "Surveys",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyID = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyVersion", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SurveyVersion_Survey_SurveyID",
                        column: x => x.SurveyID,
                        principalSchema: "Surveys",
                        principalTable: "Survey",
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
                name: "SurveyInstance",
                schema: "Surveys",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyID = table.Column<long>(type: "bigint", nullable: false),
                    SurveyVersionID = table.Column<long>(type: "bigint", nullable: false),
                    TriggeredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CustomerRef = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    MetaDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyInstance", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SurveyInstance_SurveyVersion_SurveyVersionID",
                        column: x => x.SurveyVersionID,
                        principalSchema: "Surveys",
                        principalTable: "SurveyVersion",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyInstance_Survey_SurveyID",
                        column: x => x.SurveyID,
                        principalSchema: "Surveys",
                        principalTable: "Survey",
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
                name: "SurveyResponse",
                schema: "Surveys",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyInstanceID = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AgentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyResponse", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SurveyResponse_SurveyInstance_SurveyInstanceID",
                        column: x => x.SurveyInstanceID,
                        principalSchema: "Surveys",
                        principalTable: "SurveyInstance",
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
                name: "SurveyAnswer",
                schema: "Surveys",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyResponseID = table.Column<long>(type: "bigint", nullable: false),
                    BankEntryID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    KeyAtSubmission = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyAnswer", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SurveyAnswer_BankQuestion_BankEntryID",
                        column: x => x.BankEntryID,
                        principalSchema: "Surveys",
                        principalTable: "BankQuestion",
                        principalColumn: "BankEntryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyAnswer_SurveyResponse_SurveyResponseID",
                        column: x => x.SurveyResponseID,
                        principalSchema: "Surveys",
                        principalTable: "SurveyResponse",
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
                name: "IX_BankQuestion_BankEntryID",
                schema: "Surveys",
                table: "BankQuestion",
                column: "BankEntryID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankQuestion_Key",
                schema: "Surveys",
                table: "BankQuestion",
                column: "Key",
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
                name: "IX_ScreenTemplate_Key",
                schema: "Surveys",
                table: "ScreenTemplate",
                column: "Key",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Name",
                schema: "Surveys",
                table: "Survey",
                column: "Name",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAnswer_BankEntryID",
                schema: "Surveys",
                table: "SurveyAnswer",
                column: "BankEntryID");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAnswer_KeyAtSubmission",
                schema: "Surveys",
                table: "SurveyAnswer",
                column: "KeyAtSubmission");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAnswer_SurveyResponseID",
                schema: "Surveys",
                table: "SurveyAnswer",
                column: "SurveyResponseID");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyInstance_CustomerRef",
                schema: "Surveys",
                table: "SurveyInstance",
                column: "CustomerRef");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyInstance_PublicID",
                schema: "Surveys",
                table: "SurveyInstance",
                column: "PublicID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyInstance_Status",
                schema: "Surveys",
                table: "SurveyInstance",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyInstance_SurveyID",
                schema: "Surveys",
                table: "SurveyInstance",
                column: "SurveyID");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyInstance_SurveyVersionID",
                schema: "Surveys",
                table: "SurveyInstance",
                column: "SurveyVersionID");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponse_CompletedAt",
                schema: "Surveys",
                table: "SurveyResponse",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponse_SurveyInstanceID",
                schema: "Surveys",
                table: "SurveyResponse",
                column: "SurveyInstanceID");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyVersion_Hash",
                schema: "Surveys",
                table: "SurveyVersion",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyVersion_SurveyID_Version",
                schema: "Surveys",
                table: "SurveyVersion",
                columns: new[] { "SurveyID", "Version" },
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Apps",
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
                name: "DeletedRowLogs");

            migrationBuilder.DropTable(
                name: "ScreenTemplate",
                schema: "Surveys");

            migrationBuilder.DropTable(
                name: "SurveyAnswer",
                schema: "Surveys");

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
                name: "BankQuestion",
                schema: "Surveys");

            migrationBuilder.DropTable(
                name: "SurveyResponse",
                schema: "Surveys");

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
                name: "SurveyInstance",
                schema: "Surveys");

            migrationBuilder.DropTable(
                name: "CompanyBranches",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "SurveyVersion",
                schema: "Surveys");

            migrationBuilder.DropTable(
                name: "Cities",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Companies",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Survey",
                schema: "Surveys");

            migrationBuilder.DropTable(
                name: "Regions",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "Countries",
                schema: "ShiftIdentity");
        }
    }
}
