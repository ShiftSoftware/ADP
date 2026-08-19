using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Microsoft.Azure.Cosmos;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// Declares that a source READS a Cosmos container, and which one. Purely declarative — the
/// reader itself is closed over by the ingest delegate, exactly as a SQL connection string is.
///
/// <para>It exists so <see cref="SnapshotAgentLoop"/> can refuse to start a host that has a
/// Cosmos-reading source and no Cosmos client. Without it such a source starts clean and throws
/// on every cadence tick instead — the silent-dark shape that ships well-formed EMPTY parquet a
/// consumer cannot distinguish from "this table genuinely has no rows".</para>
/// </summary>
public sealed record CosmosSourceRead(string Database, string Container)
{
    public override string ToString() => $"{Database}/{Container}";
}

/// <summary>One change-feed pull request. Null <see cref="ContinuationToken"/> starts from the beginning.</summary>
public sealed record CosmosChangeFeedRequest
{
    public required string Database { get; init; }

    public required string Container { get; init; }

    /// <summary>
    /// Resume position. Null means <c>ChangeFeedStartFrom.Beginning()</c> — a full read of the
    /// container, which is the intended bootstrap and must never be reached by accident.
    /// </summary>
    public string? ContinuationToken { get; init; }

    /// <summary>Documents per response the service should aim for. A hint, not a guarantee.</summary>
    public int PageSizeHint { get; init; } = 1000;
}

/// <summary>
/// One change-feed response.
/// </summary>
/// <param name="Documents">The changed documents, service order. Empty on the caught-up response.</param>
/// <param name="ContinuationToken">
/// The position AFTER these documents, taken off the response itself. Persisting it is what makes
/// the next read incremental.
/// </param>
/// <param name="CaughtUp">
/// True on the HTTP 304 NotModified response that ends a drain. <b>The token still matters on this
/// page</b> — 304 is where the feed reports its final position, and it arrives as an ordinary
/// response, never as an exception.
/// </param>
public sealed record CosmosChangeFeedPage(
    IReadOnlyList<JsonObject> Documents,
    string? ContinuationToken,
    bool CaughtUp,
    double RequestCharge);

/// <summary>
/// Raised when Cosmos rejects the persisted continuation token (malformed, or from a container
/// that no longer matches). The ingestor answers by discarding the cursor and re-reading from the
/// beginning, loudly — safe by construction, because the merge's hash diff makes a re-read of
/// unchanged documents a no-op, but expensive enough that it must never be silent.
/// </summary>
public sealed class CosmosChangeFeedTokenException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>
/// The READ seam onto Cosmos — deliberately separate from <c>ICosmosSnapshotTransport</c>, which
/// exposes only <c>UpsertAsync</c>/<c>DeleteAsync</c> and stays that way.
///
/// <para>It is not a symmetrical sibling of the transport, and that asymmetry is the point. The
/// write path is <c>internal</c>, visible only to the tests and the local harness, so the
/// write-only guarantee on the pump is enforced by the type system. This one is consumed by a
/// host's registry, so it is public — and adding a read here cannot widen the pump.</para>
///
/// <para>Change feed <b>pull</b> model: an ordinary query issued on the source's own cadence,
/// inside Hawta's existing loop. No Functions host, no lease container, no second copy of the
/// data. What it buys over a hand-rolled <c>_ts</c> query is ordered, resumable deltas with a
/// token instead of a watermark whose skew we would have to reason about.</para>
/// </summary>
public interface ICosmosSnapshotReader
{
    /// <summary>
    /// Streams change-feed pages until the feed reports itself caught up. The caught-up page is
    /// yielded (it carries the final token) and the sequence then ends; it never blocks waiting
    /// for new changes. The consumer may stop early — what it has merged is bounded by the last
    /// page it took.
    /// </summary>
    IAsyncEnumerable<CosmosChangeFeedPage> ReadChangeFeedAsync(
        CosmosChangeFeedRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// The production reader, over a <see cref="CosmosClient"/>.
///
/// <para>It uses the STREAM iterator rather than the typed one on purpose. The pull model reports
/// "caught up" as HTTP <b>304 NotModified</b>, and the documented way to observe that status —
/// and to take the continuation token off that same response — is the stream iterator. The typed
/// iterator hides the response, which is exactly the thing this drain must not lose.</para>
/// </summary>
public sealed class CosmosClientSnapshotReader(CosmosClient client) : ICosmosSnapshotReader
{
    public async IAsyncEnumerable<CosmosChangeFeedPage> ReadChangeFeedAsync(
        CosmosChangeFeedRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var container = client.GetContainer(request.Database, request.Container);

        ChangeFeedStartFrom startFrom;
        try
        {
            startFrom = request.ContinuationToken is null
                ? ChangeFeedStartFrom.Beginning()
                : ChangeFeedStartFrom.ContinuationToken(request.ContinuationToken);
        }
        catch (Exception exception)
        {
            // A token this SDK will not even parse. Same answer as one the service rejects.
            throw new CosmosChangeFeedTokenException(
                $"The persisted continuation token for {request.Database}/{request.Container} is not a token this " +
                "SDK can resume from.",
                exception);
        }

        using var iterator = container.GetChangeFeedStreamIterator(
            startFrom,
            ChangeFeedMode.LatestVersion,
            new ChangeFeedRequestOptions { PageSizeHint = request.PageSizeHint });

        // HasMoreResults stays true for a change feed — it is an unbounded stream, not a query.
        // The loop terminates on 304, which is the feed saying "you are current".
        while (iterator.HasMoreResults)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CosmosChangeFeedPage page;
            using (var response = await iterator.ReadNextAsync(cancellationToken))
            {
                if (response.StatusCode == HttpStatusCode.NotModified)
                {
                    yield return new CosmosChangeFeedPage(
                        [], response.ContinuationToken, CaughtUp: true, response.Headers.RequestCharge);
                    yield break;
                }

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Gone)
                    {
                        throw new CosmosChangeFeedTokenException(
                            $"Cosmos rejected the change-feed position for {request.Database}/{request.Container} " +
                            $"({(int)response.StatusCode} {response.StatusCode}): {response.ErrorMessage}");
                    }

                    throw new InvalidOperationException(
                        $"Change-feed read on {request.Database}/{request.Container} failed " +
                        $"({(int)response.StatusCode} {response.StatusCode}): {response.ErrorMessage}");
                }

                page = new CosmosChangeFeedPage(
                    ReadDocuments(response.Content),
                    response.ContinuationToken,
                    CaughtUp: false,
                    response.Headers.RequestCharge);
            }

            yield return page;
        }
    }

    /// <summary>
    /// The change-feed body is <c>{ "_rid": …, "Documents": [ … ], "_count": n }</c>. Anything
    /// else is a contract break we must not paper over: an empty list here would read as "no
    /// changes" and silently advance the cursor past documents we never saw.
    /// </summary>
    private static IReadOnlyList<JsonObject> ReadDocuments(Stream content)
    {
        var root = JsonNode.Parse(content) as JsonObject
            ?? throw new InvalidDataException("A change-feed response body was not a JSON object.");

        if (root["Documents"] is not JsonArray documents)
            throw new InvalidDataException("A change-feed response body carried no 'Documents' array.");

        var results = new List<JsonObject>(documents.Count);
        foreach (var node in documents)
        {
            results.Add(node as JsonObject
                ?? throw new InvalidDataException("A change-feed response contained a non-object document."));
        }

        return results;
    }
}
