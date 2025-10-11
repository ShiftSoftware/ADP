using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehiclePaintThicknessEvaluator
{
    private readonly CompanyDataAggregateCosmosModel CompanyDataAggregate;
    private readonly LookupOptions Options;
    private readonly IServiceProvider ServiceProvider;
    public VehiclePaintThicknessEvaluator(CompanyDataAggregateCosmosModel companyDataAggregate, LookupOptions options, IServiceProvider ServiceProvider)
    {
        this.CompanyDataAggregate = companyDataAggregate;
        this.Options = options;
        this.ServiceProvider = ServiceProvider;
    }

    public async Task<IEnumerable<PaintThicknessInspectionDTO>> Evaluate(string languageCode)
    {
        if (CompanyDataAggregate.PaintThicknessInspections is null)
            return null;

        var inspections = await Task.WhenAll(
            CompanyDataAggregate.PaintThicknessInspections.Select(async p =>
            {
                var panels = p.Panels == null
                    ? null
                    : await Task.WhenAll(
                        p.Panels.Select(async x =>
                        {
                            var images = await Task.WhenAll(
                                x.Images.Select(i => GetPaintThicknessImageFullUrl(i, languageCode))
                            );

                            return new PaintThicknessInspectionPanelDTO
                            {
                                Images = images,
                                MeasuredThickness = x.MeasuredThickness,
                                PanelPosition = x.PanelPosition,
                                PanelSide = x.PanelSide,
                                PanelType = x.PanelType,
                            };
                        })
                    );

                return new PaintThicknessInspectionDTO
                {
                    InspectionDate = p.InspectionDate,
                    Source = p.Source,
                    Panels = panels,
                };
            })
        );

        return inspections;
    }

    private async Task<string> GetPaintThicknessImageFullUrl(string image, string languageCode)
    {
        if (Options?.PaintThickneesImageUrlResolver is not null)
            return await Options.PaintThickneesImageUrlResolver(new(image, languageCode, ServiceProvider));

        return image;
    }
}