namespace ShiftSoftware.ADP.Cases.Shared.Printing;

/// <summary>
/// The company/branch identity block a printout renders (titles, logos, footer contact details).
/// Returned by <see cref="ICompanyInfoProvider"/> for every role a report references.
/// </summary>
/// <param name="Name">Display name, already resolved to the requested language.</param>
/// <param name="Address">Address line, already resolved to the requested language.</param>
/// <param name="Phone">Contact phone as printed.</param>
/// <param name="Website">Website as printed.</param>
/// <param name="Logo">Logo URL usable as a FastReport <c>PictureObject.ImageLocation</c>
/// (e.g. a signed blob URL). Null/empty renders no image.</param>
/// <param name="ShortCode">Short code (also consumed by non-print seams such as claim-number
/// auto-numbering).</param>
public record CompanyPrintInfo(
    string Name,
    string Address,
    string Phone,
    string Website,
    string? Logo,
    string ShortCode
);
