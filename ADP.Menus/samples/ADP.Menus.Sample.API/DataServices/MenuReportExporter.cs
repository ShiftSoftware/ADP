using ClosedXML.Excel;
using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

namespace ShiftSoftware.ADP.Menus.Sample.API.DataServices;

public class MenuReportExporter : IMenuReportExporter
{
    public ValueTask<byte[]> ExportMenusToExcel(MenuExportContext context)
    {
        var lines = context.Lines;
        var brandMapping = context.BrandMappings;

        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Sheet1");

        // Get the biggest number of parts in a menu code
        var numberOfParts = lines?.Select(x => x.Parts.Count())?.Max() ?? 0;

        GenerateHeadersForExportMenuToExcel(worksheet, numberOfParts);

        var index = 0;

        foreach (var line in lines ?? [])
        {
            index++;

            worksheet.Cell(index + 2, 1).Value = line.Code;
            worksheet.Cell(index + 2, 2).Value = brandMapping.GetValueOrDefault(line.BrandID)?.Code ?? string.Empty;
            worksheet.Cell(index + 2, 3).Value = line.BasicModelCode;
            worksheet.Cell(index + 2, 4).Value = "";
            worksheet.Cell(index + 2, 5).Value = line.Description;
            worksheet.Cell(index + 2, 6).Value = "S";
            worksheet.Cell(index + 2, 7).Value = "";
            worksheet.Cell(index + 2, 8).Value = "";
            worksheet.Cell(index + 2, 9).Value = line.LabourCode;

            for (int i = 0; i < numberOfParts; i++)
            {
                var part = line.Parts.ElementAtOrDefault(i);
                if (part is not null)
                {
                    worksheet.Cell(index + 2, 10 + i).Value = part.PartNumber;
                    worksheet.Cell(index + 2, 10 + i + numberOfParts).Value = part.Quantity;
                }
                else
                {
                    worksheet.Cell(index + 2, 10 + i).Value = "";
                    worksheet.Cell(index + 2, 10 + i + numberOfParts).Value = 0;
                }
            }

            worksheet.Cell(index + 2, 9 + (numberOfParts * 2) + 1).Value = "";
            worksheet.Cell(index + 2, 9 + (numberOfParts * 2) + 2).Value = line.MenuTotalPrice;
        }

        // Save the workbook to a byte array
        using var stream = new MemoryStream();

        workbook.SaveAs(stream);
        return new ValueTask<byte[]>(stream.ToArray());
    }

    public ValueTask<byte[]> ExportRTSCodesToExcel(RTSCodeExportContext context)
    {
        var items = context.Items;
        var labourRateMapping = context.LabourRateMappings;
        var brandMapping = context.BrandMappings;

        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Sheet1");

        GenerateHeadersForExportRTSCodeToExcel(worksheet);

        var index = 0;

        foreach (var item in items)
        {
            index++;

            var labourRateMap = labourRateMapping[new CompositeKey<long?, decimal>(item.BrandID, item.LabourRate)];
            var brandAbbreviation = brandMapping.GetValueOrDefault(item.BrandID)?.BrandAbbreviation ?? "Z";

            var allowedTimeText = Utility.GetAllowedTimeText(item.AllowedTime);
            var labourCode = $"{item.LabourCode}{allowedTimeText}{labourRateMap.Code}{brandAbbreviation}".Trim();

            worksheet.Cell(index + 2, 1).Value = labourCode;
            worksheet.Cell(index + 2, 2).Value = brandMapping.GetValueOrDefault(item.BrandID)?.Code ?? string.Empty;
            worksheet.Cell(index + 2, 3).Value = item.Description;
            worksheet.Cell(index + 2, 4).Value = item.Description;
            worksheet.Cell(index + 2, 5).Value = item.AllowedTime;
            worksheet.Cell(index + 2, 6).Value = "S";
            worksheet.Cell(index + 2, 7).Value = "HOUR";
            worksheet.Cell(index + 2, 8).Value = 60;
            worksheet.Cell(index + 2, 9).Value = labourRateMap.Code;
            worksheet.Cell(index + 2, 10).Value = labourCode;
            worksheet.Cell(index + 2, 11).Value = "M";
            worksheet.Cell(index + 2, 12).Value = 0;
            worksheet.Cell(index + 2, 13).Value = "";
            worksheet.Cell(index + 2, 14).Value = "-";
        }

        // Save the workbook to a byte array
        using var stream = new MemoryStream();

        workbook.SaveAs(stream);
        return new ValueTask<byte[]>(stream.ToArray());
    }

    public ValueTask<byte[]> ExportMenuDetailReportToExcel(MenuDetailReportExportContext context)
    {
        var newVersionLines = context.NewVersionLines;
        var oldVersionLines = context.OldVersionLines;
        var brandMapping = context.BrandMappings;

        var oldVersionLinesDic = oldVersionLines.ToDictionary(x => x.Code, x => x);

        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Sheet1");

        // Freeze the first row
        worksheet.SheetView.FreezeRows(1);

        // Get the biggest number of parts in a menu code
        var numberOfParts = (newVersionLines != null && newVersionLines.Any())
            ? newVersionLines.Select(x => x.Parts?.Count() ?? 0).Max()
            : 0;

        GenerateHeadersForExportMenusDetailReportToExcel(worksheet, numberOfParts);

        var index = 1;

        foreach (var line in newVersionLines ?? [])
        {
            index++;

            var oldLine = oldVersionLinesDic?.GetValueOrDefault(line.Code, null);
            var oldPriceValue = oldLine?.MenuTotalPrice;

            worksheet.Cell(index, 1).Value = line.Code; // Menu Code
            worksheet.Cell(index, 2).Value = line.BasicModelCode; // Basic Model Code
            worksheet.Cell(index, 3).Value = line.Model;    // Model

            var allowedTime = worksheet.Cell(index, 4);
            allowedTime.Value = line.AllowedTime; // Allowed Time

            var labourRate = worksheet.Cell(index, 5);
            labourRate.Value = line.LabourRate; // Labour Rate

            var labourCost = worksheet.Cell(index, 6);  // Labour Cost
            labourCost.FormulaA1 = $"{allowedTime.Address} * 10";
            labourCost.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

            var labourPrice = worksheet.Cell(index, 7); // Labour Price
            labourPrice.FormulaA1 = $"{allowedTime.Address} * {labourRate.Address}";
            labourPrice.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

            var labourProfit = worksheet.Cell(index, 8);    // Labour Profit
            labourProfit.FormulaA1 = $"{labourPrice.Address} - {labourCost.Address}";
            labourProfit.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

            var partsCost = worksheet.Cell(index, 9);
            partsCost.Value = line.PartsCost;    // Parts Cost

            var partsPrice = worksheet.Cell(index, 10);
            partsPrice.Value = line.PartsPrice;   // Parts Price

            var partsProfit = worksheet.Cell(index, 11);    // Parts Profit
            partsProfit.FormulaA1 = $"{partsPrice.Address} - {partsCost.Address}";
            partsProfit.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

            var partsProfitPercentage = worksheet.Cell(index, 12);  // Parts Profit Percentage
            partsProfitPercentage.FormulaA1 = $"IF({partsCost.Address}=0, 0, ({partsProfit.Address}/{partsCost.Address}))";
            partsProfitPercentage.Style.NumberFormat.Format = "0.00%"; // Format as percentage with two decimal digits
            partsProfitPercentage.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

            var grossProfit = worksheet.Cell(index, 13);    // Gross Profit
            grossProfit.FormulaA1 = $"{partsProfit.Address} + {labourProfit.Address}"; // Gross Profit = Parts Profit + Labour Profit
            grossProfit.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

            var grossProfitPercentage = worksheet.Cell(index, 14);  // Gross Profit Percentage
            grossProfitPercentage.FormulaA1 = $"IF({grossProfit.Address}=0, 0, ({grossProfit.Address}/({labourPrice.Address}+{partsPrice.Address})))"; // Gross Profit % = (Gross Profit / Labour Price) * 100
            grossProfitPercentage.Style.NumberFormat.Format = "0.00%"; // Format as percentage with two decimal digits
            grossProfitPercentage.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

            var consumable = worksheet.Cell(index, 15); // Consumable
            consumable.Value = line.Consumable;  // Consumable

            var labourTotalPrice = worksheet.Cell(index, 16);
            labourTotalPrice.FormulaA1 = $"{labourPrice.Address} + {consumable.Address}"; // Labour Total
            labourTotalPrice.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

            var discount = worksheet.Cell(index, 17);   // Discount %
            discount.Value = line.DiscountPercentage / 100;  // Discount %
            discount.Style.NumberFormat.Format = "0.00%";

            var oldPrice = worksheet.Cell(index, 18);
            oldPrice.Value = oldPriceValue; // Old Menu Price

            var menuTotalPrice = worksheet.Cell(index, 19); // New Menu Price
            menuTotalPrice.FormulaA1 = $"({partsPrice.Address} + {labourTotalPrice.Address}) - ({discount.Address} * ({partsPrice.Address} + {labourTotalPrice.Address}))"; // Menu Total Price = (Parts Price + Labour Total Price) - Discount
            menuTotalPrice.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

            var priceDiff = worksheet.Cell(index, 20);  // Price Difference
            priceDiff.FormulaA1 = $"{menuTotalPrice.Address} - {oldPrice.Address}"; // Price Difference
            priceDiff.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

            var priceDiffPercentage = worksheet.Cell(index, 21); // Price Difference %
            priceDiffPercentage.FormulaA1 = $"IF({oldPrice.Address}=0, \"\", ({priceDiff.Address}/{oldPrice.Address}))"; // Price Difference % = (Price Difference / Old Price) * 100
            priceDiffPercentage.Style.NumberFormat.Format = "0.00%"; // Format as percentage with two decimal digits
            priceDiffPercentage.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

            var menuProfit = worksheet.Cell(index, 22); // Menu Profit
            menuProfit.FormulaA1 = $"{labourProfit.Address} + {partsProfit.Address} + {consumable.Address}"; // Menu Profit = Labour Profit + Parts Profit + Consumalbe
            menuProfit.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

            worksheet.Cell(index, 23).Value = brandMapping.GetValueOrDefault(line.BrandID)?.Code ?? string.Empty;   // Company Code
            worksheet.Cell(index, 24).Value = line.LabourCode; // RTS Code

            var lastColumnIndex = 24;

            for (int i = 0; i < numberOfParts; i++)
            {
                var partIndex = lastColumnIndex + (i * 6);

                var part = line.Parts.ElementAtOrDefault(i);
                if (part is not null)
                {
                    worksheet.Cell(index, partIndex + 1).Value = part.PartNumber;

                    var partCost = worksheet.Cell(index, partIndex + 2);
                    partCost.Value = part.Cost;

                    var partPrice = worksheet.Cell(index, partIndex + 3);
                    partPrice.Value = part.Price;

                    var partQauntity = worksheet.Cell(index, partIndex + 4);
                    partQauntity.Value = part.Quantity;

                    var partTotalCost = worksheet.Cell(index, partIndex + 5);
                    partTotalCost.FormulaA1 = $"{partCost.Address} * {partQauntity.Address}";
                    partTotalCost.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

                    var partTotalPrice = worksheet.Cell(index, partIndex + 6);
                    partTotalPrice.FormulaA1 = $"{partPrice.Address} * {partQauntity.Address}";
                    partTotalPrice.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell
                }
                else
                {
                    worksheet.Cell(index, partIndex + 1).Value = "";
                    worksheet.Cell(index, partIndex + 2).Value = "";
                    worksheet.Cell(index, partIndex + 3).Value = "";
                    worksheet.Cell(index, partIndex + 4).Value = "";

                    var partTotalCost = worksheet.Cell(index, partIndex + 5);
                    partTotalCost.Value = "";
                    partTotalCost.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell

                    var partTotalPrice = worksheet.Cell(index, partIndex + 6);
                    partTotalPrice.Value = "";
                    partTotalPrice.Style.Fill.BackgroundColor = XLColor.LightYellow; // Highlight the cell
                }
            }
        }

        // Auto-fit columns and rows
        worksheet.Columns().AdjustToContents();
        worksheet.Rows().AdjustToContents();

        // Save the workbook to a byte array
        using var stream = new MemoryStream();

        workbook.SaveAs(stream);
        return new ValueTask<byte[]>(stream.ToArray());
    }

    private static void GenerateHeadersForExportMenuToExcel(IXLWorksheet worksheet, int numberOfParts)
    {
        worksheet.Cell(1, 1).Value = "Menu Code";
        worksheet.Cell(2, 1).Value = "menu";

        worksheet.Cell(1, 2).Value = "Company number";
        worksheet.Cell(2, 2).Value = "COMPANY";

        worksheet.Cell(1, 3).Value = "Short name";
        worksheet.Cell(2, 3).Value = "SHORT";

        worksheet.Cell(1, 4).Value = "Model group";
        worksheet.Cell(2, 4).Value = "MODELGRP";

        worksheet.Cell(1, 5).Value = "Free text";
        worksheet.Cell(2, 5).Value = "TEXT";

        worksheet.Cell(1, 6).Value = "VAT code";
        worksheet.Cell(2, 6).Value = "VATCODE";

        worksheet.Cell(1, 7).Value = "Analysis code";
        worksheet.Cell(2, 7).Value = "ANALYSIS";

        worksheet.Cell(1, 8).Value = "VAT inclusive flag";
        worksheet.Cell(2, 8).Value = "VATINCL";

        worksheet.Cell(1, 9).Value = "Labour RTS codes_1";
        worksheet.Cell(2, 9).Value = "LABOUR";

        var lastColumnIndex = 9;

        for (int i = 1; i <= numberOfParts; i++)
        {
            worksheet.Cell(1, lastColumnIndex + i).Value = $"Parts / assemblies_{i}";
            worksheet.Cell(2, lastColumnIndex + i).Value = "PARTS";

            worksheet.Cell(1, lastColumnIndex + i + numberOfParts).Value = $"Parts quantity_{i}";
            worksheet.Cell(2, lastColumnIndex + i + numberOfParts).Value = "PARTSQTY";
        }

        worksheet.Cell(1, lastColumnIndex + (numberOfParts * 2) + 1).Value = "Vspec product";
        worksheet.Cell(2, lastColumnIndex + (numberOfParts * 2) + 1).Value = "VSPEC";

        worksheet.Cell(1, lastColumnIndex + (numberOfParts * 2) + 2).Value = "Fixed price amount";
        worksheet.Cell(2, lastColumnIndex + (numberOfParts * 2) + 2).Value = "FIXEDPRI";
    }

    private static void GenerateHeadersForExportRTSCodeToExcel(IXLWorksheet worksheet)
    {
        worksheet.Cell(1, 1).Value = "RTS code";
        worksheet.Cell(2, 1).Value = "RTSCODE";

        worksheet.Cell(1, 2).Value = "Company number";
        worksheet.Cell(2, 2).Value = "COMPANY";

        worksheet.Cell(1, 3).Value = "Description";
        worksheet.Cell(2, 3).Value = "DESC1";

        worksheet.Cell(1, 4).Value = "Description";
        worksheet.Cell(2, 4).Value = "DESC2";

        worksheet.Cell(1, 5).Value = "Allowed units_1";
        worksheet.Cell(2, 5).Value = "ALLOWED";

        worksheet.Cell(1, 6).Value = "VAT rate";
        worksheet.Cell(2, 6).Value = "VATRATE";

        worksheet.Cell(1, 7).Value = "Unit of labour";
        worksheet.Cell(2, 7).Value = "UNIT";

        worksheet.Cell(1, 8).Value = "Mins per unit";
        worksheet.Cell(2, 8).Value = "MINSUNIT";

        worksheet.Cell(1, 9).Value = "Labour rates";
        worksheet.Cell(2, 9).Value = "LABRATES";

        worksheet.Cell(1, 10).Value = "Short name lookup";
        worksheet.Cell(2, 10).Value = "SHORT";

        worksheet.Cell(1, 11).Value = "Load group";
        worksheet.Cell(2, 11).Value = "LOADTYPE";

        worksheet.Cell(1, 12).Value = "Average time taken";
        worksheet.Cell(2, 12).Value = "AVGTAKEN";

        worksheet.Cell(1, 13).Value = "Labour Group";
        worksheet.Cell(2, 13).Value = "GENERIC";

        worksheet.Cell(1, 14).Value = "Overwrite lab rate";
        worksheet.Cell(2, 14).Value = "OVRLAB";
    }

    private static void GenerateHeadersForExportMenusDetailReportToExcel(IXLWorksheet worksheet, int numberOfParts)
    {
        worksheet.Cell(1, 1).Value = "Menu Code";
        worksheet.Cell(1, 2).Value = "Basic Model Code";
        worksheet.Cell(1, 3).Value = "Model";
        worksheet.Cell(1, 4).Value = "Allowed Time";
        worksheet.Cell(1, 5).Value = "Labour Rate";
        worksheet.Cell(1, 6).Value = "Labour Cost";
        worksheet.Cell(1, 7).Value = "Labour Price";
        worksheet.Cell(1, 8).Value = "Labour Profit";
        worksheet.Cell(1, 9).Value = "Parts Cost";
        worksheet.Cell(1, 10).Value = "Parts Price";
        worksheet.Cell(1, 11).Value = "Parts Profit";
        worksheet.Cell(1, 12).Value = "Parts Profit %";
        worksheet.Cell(1, 13).Value = "GP";
        worksheet.Cell(1, 14).Value = "GP %";
        worksheet.Cell(1, 15).Value = "Consumable";
        worksheet.Cell(1, 16).Value = "Labour Total";
        worksheet.Cell(1, 17).Value = "Discount %";
        worksheet.Cell(1, 18).Value = "Old Menu Price";
        worksheet.Cell(1, 19).Value = "New Menu Price";
        worksheet.Cell(1, 20).Value = "Price Difference";
        worksheet.Cell(1, 21).Value = "Price Difference %";
        worksheet.Cell(1, 22).Value = "Menu Profit";
        worksheet.Cell(1, 23).Value = "Comp";
        worksheet.Cell(1, 24).Value = "RTS Code";

        var lastColumnIndex = 24;

        for (int i = 1; i <= numberOfParts; i++)
        {
            var index = lastColumnIndex + ((i - 1) * 6);

            worksheet.Cell(1, index + 1).Value = $"Part {i}";
            worksheet.Cell(1, index + 2).Value = $"Part {i} Cost";
            worksheet.Cell(1, index + 3).Value = $"Part {i} Price";
            worksheet.Cell(1, index + 4).Value = $"Part {i} Quantity";
            worksheet.Cell(1, index + 5).Value = $"Part {i} Total Cost";
            worksheet.Cell(1, index + 6).Value = $"Part {i} Total Price";
        }

        // Set background color for the header row
        worksheet.CellsUsed().Style.Fill.BackgroundColor = XLColor.LightBlue;
    }
}
