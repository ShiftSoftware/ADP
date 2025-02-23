using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Shared;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;
using ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;
using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup
{
    public class CustomerVehicleLookupLogCosmosModel
    {
        public Guid id { get; set; }
        public string VIN { get; set; } = default!;
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public DateTimeOffset LookupDate { get; set; }
        public LookupSources? VehicleLookupSource { get; set; }
        public bool Authorized { get; set; }
        public bool HasActiveWarranty { get; set; }

        /// <summary>
        /// The Brand that the user made the lookup for
        /// </summary>
        public Brands? LookupBrand { get; set; }

        /// <summary>
        /// The Actual Vehicle Brand if it was found in the lookup service
        /// </summary>
        public Brands? OfficialVehicleBrand { get; set; }
        public long? CityId { get; set; }
    }
}
