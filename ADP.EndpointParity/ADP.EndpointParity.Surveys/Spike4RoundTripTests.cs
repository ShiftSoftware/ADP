using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.EndpointParity.Harness;
using ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;
using Xunit;

namespace ShiftSoftware.ADP.EndpointParity.Surveys;

/// <summary>
/// SPIKE-4's two mandated round-trips, pinning BOTH directions of the conditional write on
/// <c>BankQuestion.BankEntryID</c>.
///
/// <para>
/// This is the one member where the harness's sentinel-fill strategy is not enough, because
/// <c>Guid.Empty</c> is itself the MEANINGFUL input: it is the signal "I am not supplying this,
/// keep the server's value". A sentinel GUID would exercise only the write branch and would leave
/// the skip branch - the one that protects the generated default on create - completely untested.
/// </para>
///
/// <para>
/// The failure this guards against is specific: replacing <c>.Condition(...)</c> with a plain
/// <c>ForEntity</c> would overwrite the entity's <c>Guid.NewGuid()</c> default with
/// <c>Guid.Empty</c> on create, and replacing it with <c>IgnoreEntity</c> would silently drop
/// legitimate admin updates. Neither shows up as an error; both show up here.
/// </para>
/// </summary>
public class Spike4RoundTripTests
{
    private readonly ITestOutputHelper output;

    public Spike4RoundTripTests(ITestOutputHelper output) => this.output = output;

    private const string Database = "ADP_Parity_Surveys_spike4";

    private static string Master =>
        @"Server=localhost\sqlexpress;Initial Catalog=master;Integrated Security=SSPI;TrustServerCertificate=True;";

    private static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ADP.sln")))
                dir = dir.Parent;
            return dir!.FullName;
        }
    }

    private static async Task<HttpClient> BootAsync(SampleHostFactory<Program> factory, CancellationToken ct)
    {
        var client = factory.CreateClient();
        var config = factory.Services.GetRequiredService<IConfiguration>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", ParityAuth.MintToken(
            config["Settings:TokenSettings:Issuer"]!,
            config["Settings:TokenSettings:PrivateKey"]!,
            ParityAuth.BuildAccessTree(ParityGrant.FullAccess,
                new[] { "ShiftIdentityActions", "AzureStorageActionTree", "GeneralActionTree", "SurveysActionTree" },
                new Dictionary<string, int[]>())));
        await Task.CompletedTask;
        return client;
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Empty_guid_on_create_leaves_the_server_generated_default_intact()
    {
        var ct = TestContext.Current.CancellationToken;
        await ParitySeeder.ResetDatabaseAsync(Master, Database, ct);

        await using var factory = new SampleHostFactory<Program>("ConnectionStrings:SQLServer", Database);
        var client = await BootAsync(factory, ct);

        // Guid.Empty is the "not supplying this" signal. The condition must SKIP the write so the
        // entity's own Guid.NewGuid() default survives.
        var body = """
        {
          "key": "SPIKE4-EMPTY",
          "question": { "type": "text", "id": "q-spike4-empty", "title": { "en": "spike4 empty" }, "required": false },
          "bankEntryID": "00000000-0000-0000-0000-000000000000",
          "retired": false
        }
        """;

        var created = await client.PostAsync("/api/Surveys/BankQuestion", Json(body), ct);
        var createdBody = await created.Content.ReadAsStringAsync(ct);
        output.WriteLine($"POST -> {(int)created.StatusCode}");
        Assert.True(created.IsSuccessStatusCode, "create must succeed: " + createdBody);

        var entity = JsonDocument.Parse(createdBody).RootElement.GetProperty("Entity");
        var bankEntryId = entity.GetProperty("BankEntryID").GetString();
        output.WriteLine($"BankEntryID after create with Guid.Empty: {bankEntryId}");

        Assert.False(string.IsNullOrEmpty(bankEntryId));
        Assert.NotEqual("00000000-0000-0000-0000-000000000000", bankEntryId);
    }

    [Fact]
    public async Task A_different_guid_on_update_is_ATTEMPTED_not_silently_ignored()
    {
        // THIS IS THE DISCRIMINATOR between the two wrong implementations, and it is why the test
        // asserts a FAILURE rather than a success.
        //
        // BankQuestion.BankEntryID turns out to be part of a KEY - SurveyAnswer.BankEntryID carries
        // a foreign key to it - so EF refuses to modify it on a tracked entity: "The property
        // 'BankQuestion.BankEntryID' is part of a key and so cannot be modified". That is a SCHEMA
        // constraint, not a mapper one, and it applied equally to the AutoMapper profile this
        // replaced. The old profile's comment about "still allow updates from authenticated admin
        // flows" was therefore aspirational: the database has never permitted it.
        //
        // What that leaves is a clean two-way probe of the conditional:
        //   IgnoreEntity            -> the write is never attempted -> request SUCCEEDS, value silently unchanged
        //   ForEntity (conditional) -> the write IS attempted       -> EF rejects it -> request FAILS
        // So a failure here is the PASS condition: it proves the write branch is live. Paired with
        // the Guid.Empty test above, which proves the skip branch is live, both directions of
        // .Condition(...) are now pinned.
        var ct = TestContext.Current.CancellationToken;
        await ParitySeeder.ResetDatabaseAsync(Master, Database + "_2", ct);

        await using var factory = new SampleHostFactory<Program>("ConnectionStrings:SQLServer", Database + "_2");
        var client = await BootAsync(factory, ct);

        var createBody = """
        {
          "key": "SPIKE4-EXPLICIT",
          "question": { "type": "text", "id": "q-spike4-explicit", "title": { "en": "spike4 explicit" }, "required": false },
          "bankEntryID": "00000000-0000-0000-0000-000000000000",
          "retired": false
        }
        """;
        var created = await client.PostAsync("/api/Surveys/BankQuestion", Json(createBody), ct);
        var createdBody = await created.Content.ReadAsStringAsync(ct);
        Assert.True(created.IsSuccessStatusCode, "create must succeed: " + createdBody);

        var entity = JsonDocument.Parse(createdBody).RootElement.GetProperty("Entity");
        var id = entity.GetProperty("ID").GetString();
        var generated = entity.GetProperty("BankEntryID").GetString();
        var lastSave = entity.GetProperty("LastSaveDate").GetRawText();
        output.WriteLine($"server-generated BankEntryID: {generated}");

        const string differentGuid = "c0ffee00-0000-4000-8000-00000000beef";
        var updateBody = $$"""
        {
          "id": "{{id}}",
          "key": "SPIKE4-EXPLICIT",
          "question": { "type": "text", "id": "q-spike4-explicit", "title": { "en": "spike4 explicit" }, "required": false },
          "bankEntryID": "{{differentGuid}}",
          "retired": false,
          "lastSaveDate": {{lastSave}}
        }
        """;

        var updated = await client.PutAsync($"/api/Surveys/BankQuestion/{id}", Json(updateBody), ct);
        var updatedBody = await updated.Content.ReadAsStringAsync(ct);
        output.WriteLine($"PUT with a DIFFERENT guid -> {(int)updated.StatusCode}");

        Assert.False(updated.IsSuccessStatusCode,
            "the conditional write must be ATTEMPTED. A success here would mean the member is being " +
            "ignored rather than conditionally written, i.e. IgnoreEntity semantics. Body: " + updatedBody);
        Assert.Contains("part of a key", updatedBody);
    }
}
