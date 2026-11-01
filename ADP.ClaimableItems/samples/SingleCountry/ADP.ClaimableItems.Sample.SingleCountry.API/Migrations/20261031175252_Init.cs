using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.ClaimableItems.Sample.SingleCountry.API.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ClaimableItems");

            migrationBuilder.EnsureSchema(
                name: "Cases");

            migrationBuilder.CreateTable(
                name: "Campaign",
                schema: "ClaimableItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UniqueReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Brands = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Companies = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Countries = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActivationTrigger = table.Column<int>(type: "int", nullable: false),
                    ActivationType = table.Column<int>(type: "int", nullable: false),
                    CertificatePrintoutHeader = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificatePrintoutBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificatePrintoutSignStampVisibility = table.Column<bool>(type: "bit", nullable: false),
                    CertificatePrintoutKatashikiAndModelColumnVisibility = table.Column<bool>(type: "bit", nullable: false),
                    DistributorCertificateNumberPrefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehicleInspectionTypeID = table.Column<long>(type: "bigint", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    UniqueHash = table.Column<byte[]>(type: "BINARY(32)", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaign", x => x.ID);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CampaignHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ClaimableItems")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Certificates",
                schema: "Cases",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CertificateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CertificateNo = table.Column<long>(type: "bigint", nullable: false),
                    DistributorCertificateNo = table.Column<long>(type: "bigint", nullable: true),
                    DisplayDistributorCertificateNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateType = table.Column<int>(type: "int", nullable: false),
                    SettlementType = table.Column<int>(type: "int", nullable: false),
                    CampaignID = table.Column<long>(type: "bigint", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.ID);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CertificatesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Cases")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "ClaimableItemContract",
                schema: "ClaimableItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VIN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Katashiki = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasicModelCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Variant = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrandID = table.Column<long>(type: "bigint", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractType = table.Column<int>(type: "int", nullable: false),
                    Mileage = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    DurationUnit = table.Column<int>(type: "int", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransferRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MenuLanguage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PriceIncrement = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalWithIncrement = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDraft = table.Column<bool>(type: "bit", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CancelledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledByUserID = table.Column<long>(type: "bigint", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CountryID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimableItemContract", x => x.ID);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClaimableItemContractHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ClaimableItems")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "CampaignVinEntry",
                schema: "ClaimableItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VIN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CampaignID = table.Column<long>(type: "bigint", nullable: false),
                    RecordedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: true),
                    CountryID = table.Column<long>(type: "bigint", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignVinEntry", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CampaignVinEntry_Campaign_CampaignID",
                        column: x => x.CampaignID,
                        principalSchema: "ClaimableItems",
                        principalTable: "Campaign",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CampaignVinEntryHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ClaimableItems")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "ClaimableItem",
                schema: "ClaimableItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrintoutTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrintoutDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaximumMileage = table.Column<long>(type: "bigint", nullable: true),
                    ProgramRole = table.Column<int>(type: "int", nullable: false),
                    UniqueReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PackageCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidityMode = table.Column<int>(type: "int", nullable: false),
                    ClaimingMethod = table.Column<int>(type: "int", nullable: false),
                    ActiveFor = table.Column<int>(type: "int", nullable: false),
                    ActiveForDurationType = table.Column<int>(type: "int", nullable: false),
                    CostingType = table.Column<int>(type: "int", nullable: false),
                    AttachmentFieldBehavior = table.Column<int>(type: "int", nullable: false),
                    FixedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Costs = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EligibilityConditions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignID = table.Column<long>(type: "bigint", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    UniqueHash = table.Column<byte[]>(type: "BINARY(32)", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimableItem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ClaimableItem_Campaign_CampaignID",
                        column: x => x.CampaignID,
                        principalSchema: "ClaimableItems",
                        principalTable: "Campaign",
                        principalColumn: "ID");
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClaimableItemHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ClaimableItems")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "ClaimableItemContractLine",
                schema: "ClaimableItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimableItemContractID = table.Column<long>(type: "bigint", nullable: false),
                    MenuLineKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MenuCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LabourCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VariantID = table.Column<long>(type: "bigint", nullable: true),
                    VariantName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LineType = table.Column<int>(type: "int", nullable: false),
                    ServiceIntervalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceIntervalValueInMeter = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LabourTotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PartsTotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceWithIncrement = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    DurationUnit = table.Column<int>(type: "int", nullable: true),
                    ExpireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CancelledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledByUserID = table.Column<long>(type: "bigint", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimableItemContractLine", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ClaimableItemContractLine_ClaimableItemContract_ClaimableItemContractID",
                        column: x => x.ClaimableItemContractID,
                        principalSchema: "ClaimableItems",
                        principalTable: "ClaimableItemContract",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClaimableItemContractLineHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ClaimableItems")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "ItemClaim",
                schema: "ClaimableItems",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VIN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClaimDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    JobNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QRCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Katashiki = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignID = table.Column<long>(type: "bigint", nullable: false),
                    ClaimableItemID = table.Column<long>(type: "bigint", nullable: false),
                    VehicleInspectionResultID = table.Column<long>(type: "bigint", nullable: true),
                    CampaignVinEntryID = table.Column<long>(type: "bigint", nullable: true),
                    ClaimableItemContractID = table.Column<long>(type: "bigint", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PackageCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: true),
                    ReimbursementCertificateID = table.Column<long>(type: "bigint", nullable: true),
                    ContributionCertificateID = table.Column<long>(type: "bigint", nullable: true),
                    ClaimNumber = table.Column<int>(type: "int", nullable: false),
                    ClaimStatus = table.Column<int>(type: "int", nullable: true),
                    DistributorErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Attachments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasAttachment = table.Column<bool>(type: "bit", nullable: false),
                    ProcessDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastReplicationStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    UniqueHash = table.Column<byte[]>(type: "BINARY(32)", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemClaim", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ItemClaim_CampaignVinEntry_CampaignVinEntryID",
                        column: x => x.CampaignVinEntryID,
                        principalSchema: "ClaimableItems",
                        principalTable: "CampaignVinEntry",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_ItemClaim_Campaign_CampaignID",
                        column: x => x.CampaignID,
                        principalSchema: "ClaimableItems",
                        principalTable: "Campaign",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemClaim_Certificates_ContributionCertificateID",
                        column: x => x.ContributionCertificateID,
                        principalSchema: "Cases",
                        principalTable: "Certificates",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_ItemClaim_Certificates_ReimbursementCertificateID",
                        column: x => x.ReimbursementCertificateID,
                        principalSchema: "Cases",
                        principalTable: "Certificates",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_ItemClaim_ClaimableItemContract_ClaimableItemContractID",
                        column: x => x.ClaimableItemContractID,
                        principalSchema: "ClaimableItems",
                        principalTable: "ClaimableItemContract",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_ItemClaim_ClaimableItem_ClaimableItemID",
                        column: x => x.ClaimableItemID,
                        principalSchema: "ClaimableItems",
                        principalTable: "ClaimableItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ItemClaimHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ClaimableItems")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_Campaign_UniqueHash",
                schema: "ClaimableItems",
                table: "Campaign",
                column: "UniqueHash",
                unique: true,
                filter: "UniqueHash IS NOT NULL and IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignVinEntry_CampaignID",
                schema: "ClaimableItems",
                table: "CampaignVinEntry",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimableItem_CampaignID",
                schema: "ClaimableItems",
                table: "ClaimableItem",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimableItem_UniqueHash",
                schema: "ClaimableItems",
                table: "ClaimableItem",
                column: "UniqueHash",
                unique: true,
                filter: "UniqueHash IS NOT NULL and IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimableItemContractLine_ClaimableItemContractID",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine",
                column: "ClaimableItemContractID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemClaim_CampaignID",
                schema: "ClaimableItems",
                table: "ItemClaim",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemClaim_CampaignVinEntryID",
                schema: "ClaimableItems",
                table: "ItemClaim",
                column: "CampaignVinEntryID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemClaim_ClaimableItemContractID",
                schema: "ClaimableItems",
                table: "ItemClaim",
                column: "ClaimableItemContractID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemClaim_ClaimableItemID",
                schema: "ClaimableItems",
                table: "ItemClaim",
                column: "ClaimableItemID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemClaim_ContributionCertificateID",
                schema: "ClaimableItems",
                table: "ItemClaim",
                column: "ContributionCertificateID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemClaim_ReimbursementCertificateID",
                schema: "ClaimableItems",
                table: "ItemClaim",
                column: "ReimbursementCertificateID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemClaim_UniqueHash",
                schema: "ClaimableItems",
                table: "ItemClaim",
                column: "UniqueHash",
                unique: true,
                filter: "UniqueHash IS NOT NULL and IsDeleted = 0");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[Cases].[CertificatesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_CertificatesHistory_ID] ON [Cases].[CertificatesHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ClaimableItems].[CampaignHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_CampaignHistory_ID] ON [ClaimableItems].[CampaignHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ClaimableItems].[CampaignVinEntryHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_CampaignVinEntryHistory_ID] ON [ClaimableItems].[CampaignVinEntryHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ClaimableItems].[ClaimableItemContractHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_ClaimableItemContractHistory_ID] ON [ClaimableItems].[ClaimableItemContractHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ClaimableItems].[ClaimableItemContractLineHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_ClaimableItemContractLineHistory_ID] ON [ClaimableItems].[ClaimableItemContractLineHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ClaimableItems].[ClaimableItemHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_ClaimableItemHistory_ID] ON [ClaimableItems].[ClaimableItemHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ClaimableItems].[ItemClaimHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_ItemClaimHistory_ID] ON [ClaimableItems].[ItemClaimHistory] ([ID]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimableItemContractLine",
                schema: "ClaimableItems")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClaimableItemContractLineHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ClaimableItems")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "ItemClaim",
                schema: "ClaimableItems")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ItemClaimHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ClaimableItems")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "CampaignVinEntry",
                schema: "ClaimableItems")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CampaignVinEntryHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ClaimableItems")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Certificates",
                schema: "Cases")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CertificatesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Cases")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "ClaimableItemContract",
                schema: "ClaimableItems")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClaimableItemContractHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ClaimableItems")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "ClaimableItem",
                schema: "ClaimableItems")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClaimableItemHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ClaimableItems")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Campaign",
                schema: "ClaimableItems")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CampaignHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ClaimableItems")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CertificatesHistory_ID' AND object_id = OBJECT_ID(N'[Cases].[CertificatesHistory]')) DROP INDEX [IX_CertificatesHistory_ID] ON [Cases].[CertificatesHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CampaignHistory_ID' AND object_id = OBJECT_ID(N'[ClaimableItems].[CampaignHistory]')) DROP INDEX [IX_CampaignHistory_ID] ON [ClaimableItems].[CampaignHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CampaignVinEntryHistory_ID' AND object_id = OBJECT_ID(N'[ClaimableItems].[CampaignVinEntryHistory]')) DROP INDEX [IX_CampaignVinEntryHistory_ID] ON [ClaimableItems].[CampaignVinEntryHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClaimableItemContractHistory_ID' AND object_id = OBJECT_ID(N'[ClaimableItems].[ClaimableItemContractHistory]')) DROP INDEX [IX_ClaimableItemContractHistory_ID] ON [ClaimableItems].[ClaimableItemContractHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClaimableItemContractLineHistory_ID' AND object_id = OBJECT_ID(N'[ClaimableItems].[ClaimableItemContractLineHistory]')) DROP INDEX [IX_ClaimableItemContractLineHistory_ID] ON [ClaimableItems].[ClaimableItemContractLineHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClaimableItemHistory_ID' AND object_id = OBJECT_ID(N'[ClaimableItems].[ClaimableItemHistory]')) DROP INDEX [IX_ClaimableItemHistory_ID] ON [ClaimableItems].[ClaimableItemHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItemClaimHistory_ID' AND object_id = OBJECT_ID(N'[ClaimableItems].[ItemClaimHistory]')) DROP INDEX [IX_ItemClaimHistory_ID] ON [ClaimableItems].[ItemClaimHistory];");
        }
    }
}
