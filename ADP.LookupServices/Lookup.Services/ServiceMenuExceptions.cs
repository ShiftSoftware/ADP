using System;

namespace ShiftSoftware.ADP.Lookup.Services;

/// <summary>
/// Thrown when the <c>ServiceMenus</c> container (or the database holding it) does not exist.
///
/// This is a provisioning fault, not a data one, so it is raised rather than folded into an empty
/// result. Silently reporting "this model has no menu" would make an unprovisioned deployment look
/// like one whose catalog is simply empty — indistinguishable, and permanent.
/// Remediation: run <c>MenuCosmosProvisioning.EnsureContainersAsync</c> (ADP.Menus.Data) from the
/// host's deployment path, then a full catch-up sweep to populate it.
/// </summary>
public class ServiceMenuContainerNotFoundException : Exception
{
    public string DatabaseName { get; }
    public string ContainerName { get; }

    public ServiceMenuContainerNotFoundException(string databaseName, string containerName, Exception innerException)
        : base(
            $"Cosmos container '{containerName}' was not found in database '{databaseName}'. " +
            "Service-menu lookups read a single partition of that container, so it must exist before any " +
            "lookup runs. Provision it with MenuCosmosProvisioning.EnsureContainersAsync (ADP.Menus.Data), " +
            "which also verifies every partition key, then run a full catch-up replication to populate it.",
            innerException)
    {
        DatabaseName = databaseName;
        ContainerName = containerName;
    }

    /// <summary>
    /// For non-Cosmos stores, whose remediation is different: the same "never provisioned" fault, with
    /// a message that says how THIS backend gets provisioned (e.g. run the menu DuckDB sync) instead of
    /// pointing at Cosmos provisioning.
    /// </summary>
    public ServiceMenuContainerNotFoundException(string databaseName, string containerName, string message, Exception innerException)
        : base(message, innerException)
    {
        DatabaseName = databaseName;
        ContainerName = containerName;
    }
}

/// <summary>
/// Thrown when a model's replicated documents cannot produce menu lines because reference data they
/// depend on is missing — a labour-rate mapping for the variant's (brand, primary rate) pair, or a
/// service interval / interval group that no document in the partition carries.
///
/// The shared generator raises <see cref="System.Collections.Generic.KeyNotFoundException"/> for these
/// and that behaviour is deliberate (open item O1): the DMS export fails on them too, and softening it
/// would mean issuing a menu code composed from data that is not there. This wraps it so the failure
/// names the model being looked up instead of arriving as a bare dictionary miss from inside a fold.
/// Remediation: re-run catch-up replication for the model; if it persists, the source catalogue itself
/// is missing the mapping and the export would refuse to run too.
/// </summary>
public class ServiceMenuGenerationException : Exception
{
    public string BasicModelCode { get; }

    public ServiceMenuGenerationException(string basicModelCode, Exception innerException)
        : base(
            $"Service-menu generation failed for basic model code '{basicModelCode}': the replicated documents " +
            "reference master data that is not present in the partition (a labour-rate mapping for the variant's " +
            "brand and primary labour rate, or a service interval / interval group). Re-run catch-up replication " +
            "for this model; if it persists, the source catalogue is missing the row and the DMS export would " +
            "refuse to generate too.",
            innerException)
    {
        BasicModelCode = basicModelCode;
    }
}

/// <summary>
/// Thrown when a non-Cosmos menu store exists but cannot be read — a DuckDB query failure, an
/// unreadable snapshot file, a schema the reader does not understand.
///
/// This is the storage-neutral counterpart of a <c>CosmosException</c>: the Cosmos reader lets the
/// SDK's own exception surface, and callers that contain storage faults (the vehicle lookup's menu
/// section) catch both so a menu backend fault degrades to <c>Unavailable</c> on either storage
/// instead of failing the surrounding lookup. Distinct from
/// <see cref="ServiceMenuContainerNotFoundException"/>, which means the store was never provisioned
/// at all — that one has a remediation, this one is a fault to investigate.
/// </summary>
public class ServiceMenuStorageException : Exception
{
    public ServiceMenuStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
