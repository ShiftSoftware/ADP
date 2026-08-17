using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>One distinct service code and how many labour lines carry it.</summary>
[Docable]
public class ServiceCodeSample
{
    public ServiceCodeSample()
    {
    }

    public ServiceCodeSample(string code, long lines = 1)
    {
        Code = code;
        Lines = lines;
    }

    /// <summary>The code.</summary>
    public string Code { get; set; }

    /// <summary>
    /// How many labour lines carry it. Weighting the corpus by volume is what separates a shape the
    /// estate is built on from one authored once and never used.
    /// </summary>
    public long Lines { get; set; } = 1;
}
