using System;
using ShiftSoftware.ADP.Models.DealerData;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class TiqExtendedWarrantySopCosmosModel
{
    public string id { get; set; } = default!;

    public string VIN { get; set; } = default!;

    public string ItemType => "TiqExtendedWarrantySop";

    public long ExtendedWarrantySopid { get; set; }

    public long? UserId { get; set; }

    public string Response { get; set; }

    public string ResponseType { get; set; }

    public long? DivisionId { get; set; }

    public long? BranchId { get; set; }

    public long SeqId { get; set; }

    public string ExtraAttachments { get; set; }

    public string ExtraAttachmentsFiles { get; set; }

    public bool Deleted { get; set; }

    public DateTime? DeletedDate { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? InspectionDate { get; set; }

    public string WorkshopLocation { get; set; }

    public string DealerName { get; set; }

    public string VehicleModel { get; set; }

    public string ModelYear { get; set; }

    public string Color { get; set; }

    public string Vin { get; set; }

    public DateTime? LastServiceDate { get; set; }

    public string CurrentMilage { get; set; }

    public string VehicleSourcingRetail { get; set; }

    public string VehicleSourcingFleet { get; set; }

    public string VehicleSourcingExDemo { get; set; }

    public string VehicleSourcingExLease { get; set; }

    public string VehicleSourcingAuction { get; set; }

    public string VehicleSourcingWholesale { get; set; }

    public string VehicleSourcingOthers { get; set; }

    public byte IsimportedByTiq { get; set; }

    public byte IsageLess3 { get; set; }

    public byte IsmilageLess100K { get; set; }

    public byte IsqualityRating45 { get; set; }

    public byte IsfreeDamage { get; set; }

    public byte IsfreeTampering { get; set; }

    public byte IsfreeTechnical { get; set; }

    public byte IsserviceHistory { get; set; }

    public byte Isvinmatch { get; set; }

    public byte IsownersManual { get; set; }

    public byte IswarrantyBooklet { get; set; }

    public byte IssystemManual { get; set; }

    public byte IskeyCountMatch { get; set; }

    public byte IsrecallIncomingUpdate { get; set; }

    public byte Isroadworthiness { get; set; }

    public byte IswirelessTransmitter { get; set; }

    public byte Isdoor { get; set; }

    public byte IssmartEntry { get; set; }

    public byte IsvehicleHeightLevel { get; set; }

    public byte IsunlockingHoodFuel { get; set; }

    public byte IshoodHydraulic { get; set; }

    public byte IsbatteryHealth { get; set; }

    public byte IscorneringLight { get; set; }

    public byte Islight { get; set; }

    public byte IsfrontFogLight { get; set; }

    public byte IsstepLight { get; set; }

    public byte IspowerSeat { get; set; }

    public byte IsrearSeat { get; set; }

    public byte IsmanualSeat { get; set; }

    public byte IschildSafety { get; set; }

    public byte IsdoorFunction { get; set; }

    public byte IsfuelLid { get; set; }

    public byte IstailGateBackDoor { get; set; }

    public byte IstailGateHydraulic { get; set; }

    public byte IsspareTire { get; set; }

    public byte IsrearCombination { get; set; }

    public byte IslicensePlateLight { get; set; }

    public byte Isluggage { get; set; }

    public byte IsconnectGts { get; set; }

    public byte IscombinationMeter { get; set; }

    public byte IsmalfunctionIndication { get; set; }

    public byte IsengineCranking { get; set; }

    public byte IsengineRpm { get; set; }

    public byte IsengineGts { get; set; }

    public byte IsengineNoise { get; set; }

    public byte IsindicationLamp { get; set; }

    public byte IsheadUpDisplay { get; set; }

    public byte IswiperIntermittent { get; set; }

    public byte IswipingCleanliness { get; set; }

    public byte IswasherJet { get; set; }

    public byte IsheadLamp { get; set; }

    public byte IssteeringSwitchFunction { get; set; }

    public byte IssteeringManual { get; set; }

    public byte IsaudioFunction { get; set; }

    public byte IsmediaPlayer { get; set; }

    public byte IspowerOutlet12 { get; set; }

    public byte IsglowBoxLead { get; set; }

    public byte IsgloveBox { get; set; }

    public byte IsslidingRoof { get; set; }

    public byte IspwoneTouch { get; set; }

    public byte Ispwlock { get; set; }

    public byte IscenterLock { get; set; }

    public byte IssideMirror { get; set; }

    public byte IstailGateInterior { get; set; }

    public byte IsinteriorIllumination { get; set; }

    public byte IsdoorCourtesy { get; set; }

    public byte Isacoperation { get; set; }

    public byte IsairflowSpeed { get; set; }

    public byte IsblowerMotor { get; set; }

    public byte IsgrillTemperature { get; set; }

    public decimal GrillTemperature { get; set; }

    public byte IsairTemperature { get; set; }

    public byte Isacperformance { get; set; }

    public byte IsairMode { get; set; }

    public byte IsairRecirculation { get; set; }

    public byte Is4zoneAc { get; set; }

    public byte Isacsound { get; set; }

    public byte IsseatClimateControl { get; set; }

    public byte IswindshieldHeating { get; set; }

    public byte IsambientTemperature { get; set; }

    public byte Issrswarning { get; set; }

    public byte IsrollSensing { get; set; }

    public byte IsseatBelt { get; set; }

    public byte IsseatBeltInertiaLocking { get; set; }

    public byte IsunbuckledWarning { get; set; }

    public byte IslightBlinkingBefore { get; set; }

    public byte IslightStayBlink { get; set; }

    public byte IswarningSiren { get; set; }

    public byte IsintrusionSensor { get; set; }

    public byte IsdriverSide { get; set; }

    public byte IsaftermarketSeatCovers { get; set; }

    public byte IsengineRadiatorBattery { get; set; }

    public byte IsengineOil { get; set; }

    public byte Iscoolant { get; set; }

    public byte IsbrakeFluid { get; set; }

    public byte IssteeringFluid { get; set; }

    public byte IscoolantHoses { get; set; }

    public byte IsfanMotor { get; set; }

    public byte IssteeringPump { get; set; }

    public byte IscoolantBrakeSteering { get; set; }

    public byte IsbrakeMaster { get; set; }

    public byte IsengineValve { get; set; }

    public byte Isacfilter { get; set; }

    public byte IsdriveBelt { get; set; }

    public byte IswiringHarness { get; set; }

    public byte Ishevinverter { get; set; }

    public byte IstireSpecification { get; set; }

    public byte Isyear5Tire { get; set; }

    public int Year5Tire { get; set; }

    public byte IstireDepth { get; set; }

    public decimal TireDepthFr { get; set; }

    public decimal TireDepthFl { get; set; }

    public decimal TireDepthRr { get; set; }

    public decimal TireDepthRl { get; set; }

    public decimal TireDepthSp { get; set; }

    public byte IswornOutPattern { get; set; }

    public bool IswornOutPatternInner { get; set; }

    public bool IswornOutPatternOuter { get; set; }

    public bool IswornOutPatternMiddle { get; set; }

    public bool IswornOutPatternInnerOuter { get; set; }

    public byte Iscracks { get; set; }

    public byte IsbrakeCaliperJam { get; set; }

    public byte IsfrontBrakePadThickness { get; set; }

    public decimal FrontBrakePadThickness { get; set; }

    public byte IsrearBrakePadThickness { get; set; }

    public decimal RearBrakePadThickness { get; set; }

    public byte IsbrakeDisc { get; set; }

    public decimal BrakeDiscFr { get; set; }

    public decimal BrakeDiscFl { get; set; }

    public decimal BrakeDiscRr { get; set; }

    public decimal BrakeDiscRl { get; set; }

    public byte IsbrakeShoe { get; set; }

    public decimal BrakeShoeThickness { get; set; }

    public byte IsunderneathCover { get; set; }

    public byte IswiperWasherTank { get; set; }

    public byte IsengineOilFilter { get; set; }

    public byte IscrankSeal { get; set; }

    public byte IsengineOilCooler { get; set; }

    public byte IsbootBloating { get; set; }

    public byte IstransmissionShaftRear { get; set; }

    public byte Isatflevel { get; set; }

    public byte IshosesBloating { get; set; }

    public byte IstransmissionOil { get; set; }

    public byte IstransferCase { get; set; }

    public byte IsoilLevel { get; set; }

    public byte IspropellerShaft { get; set; }

    public byte IssuspensionFluid { get; set; }

    public byte IsfuelPipe { get; set; }

    public byte IsbrakePipe { get; set; }

    public byte Ishevwire { get; set; }

    public byte IsexhaustPipe { get; set; }

    public byte IsbodyMounting { get; set; }

    public byte IstransmissionMounting { get; set; }

    public byte IssuspensionDamage { get; set; }

    public byte IsbodyDamage { get; set; }

    public byte IsflooringDamage { get; set; }

    public byte IshousingDamage { get; set; }

    public byte IswheelBearing { get; set; }

    public byte IsfrontSuspension { get; set; }

    public byte IsshockAbsorber { get; set; }

    public byte IsstabilizerBushing { get; set; }

    public byte IsrackLeakage { get; set; }

    public byte IswheelBearingRoughness { get; set; }

    public byte IssuspensionTrailing { get; set; }

    public byte IsshockAbsorberLeakage { get; set; }

    public byte IsstabilizerBushingLeakage { get; set; }

    public byte IsadaptiveSuspension { get; set; }

    public byte IsfreePlay { get; set; }

    public byte IspowerOutPut { get; set; }

    public byte IsdriveMode { get; set; }

    public byte Isevmode { get; set; }

    public byte IsvehicleStability { get; set; }

    public byte IsshiftShocks { get; set; }

    public byte IsautoTransmission { get; set; }

    public byte IsmanualTransmission { get; set; }

    public byte IsbrakeEfficiency { get; set; }

    public byte IsparkingBrake { get; set; }

    public byte IsassistSteering { get; set; }

    public byte IsvehicleVibration { get; set; }

    public byte IswindNoise { get; set; }

    public byte IsrattlingNoise { get; set; }

    public byte IssuspensionNoise { get; set; }

    public byte IsbrakeSound { get; set; }

    public byte IsabnormalSound { get; set; }

    public byte IsheightControl { get; set; }

    public byte IseasyAccess { get; set; }

    public byte Is4wdcrawl { get; set; }

    public byte IsmultiTerrain { get; set; }

    public byte Istraction { get; set; }

    public byte Isabsactivation { get; set; }

    public byte IsdownHill { get; set; }

    public byte IscruiseControl { get; set; }

    public byte IslaneDeparture { get; set; }

    public byte IsblindSpot { get; set; }

    public byte IsrearCrossTraffic { get; set; }

    public byte Is360camera { get; set; }

    public byte IsrearCamera { get; set; }

    public byte IsstopStart { get; set; }

    public byte IstripMeterAb { get; set; }

    public string TechnicianName { get; set; }

    public DateTime? TechnicianDate { get; set; }

    public string ServiceManagerName { get; set; }

    public DateTime? ServiceManagerDate { get; set; }

    public string UselectName { get; set; }

    public DateTime? UselectDate { get; set; }

    public long? UsedCarId { get; set; }
}
