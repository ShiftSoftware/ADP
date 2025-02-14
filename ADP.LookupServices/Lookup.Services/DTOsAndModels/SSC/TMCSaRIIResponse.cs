namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.SSC;

public class TMCSaRIIResponse
{
    public SSCLookupStatuses SSCLookupStatus { get; set; }
    public int TMCErrorCode { get; set; }
    public string? ExceptionMessage { get; set; }
}
