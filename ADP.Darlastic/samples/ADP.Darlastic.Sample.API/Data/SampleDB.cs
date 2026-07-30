using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Darlastic.Sample.API.Data;

/// <summary>
/// The sample host's context. Deliberately empty of entities of its own: the Darlastic model
/// contributor registered by <c>AddDarlasticApiServices</c> adds the registry tables and the
/// golden view, which is exactly what a real host (TCA Tickets, TIQ Customers) gets.
///
/// <para><b>No migrations here, on purpose.</b> The module ships no DDL and hosts own their schema;
/// this sample points at a registry the engine already created (<c>dotnet run resolve</c> in the
/// spike). Calling EnsureCreated would put a second schema authority against the same database —
/// the exact failure the engine's <c>DARLASTIC_SCHEMA_MANAGED</c> switch exists to prevent.</para>
/// </summary>
public class SampleDB : ShiftDbContext
{
    public SampleDB(DbContextOptions<SampleDB> options) : base(options)
    {
    }
}
