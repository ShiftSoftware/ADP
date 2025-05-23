﻿using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

[TypeScriptModel]
public class BranchDeadStockDTO
{
    public string CompanyBranchIntegrationID { get; set; }
    public string CompanyBranchName { get; set; }
    public int Quantity { get; set; }
}