using CsvHelper.Configuration.Attributes;
using ShiftSoftware.ADP.SyncAgent;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Extensions;
using ShiftSoftware.ADP.SyncAgent.Services;

namespace ADP.SyncAgent.Tests;

public class CsvSourceValidationTests
{
    public class Row
    {
        [Index(0)] public string Id { get; set; } = "";
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Whole_source_validation_preserves_first_row_and_add_reader_header(bool full)
    {
        using var temp = new TempDirectory();
        var file = temp.File("rows.csv");
        File.WriteAllText(file, "Id\nFIRST\nSECOND\n");
        var options = new FileSystemStorageOptions
        {
            SourceBasePath = Path.GetDirectoryName(file)!,
            DestinationBasePath = temp.File("destination"),
            CompareWorkingDirectory = temp.File("diff"),
        };
        var engine = new SyncEngine<Row, Row>();
        engine.Configure([SyncActionType.Delete, SyncActionType.Add], 100, 0, 30);
        new CsvHelperCsvSyncDataSource<Row, Row>(options, new FileSystemStorageService(options))
            .SetSyncService(engine).Configure(new()
            {
                CSVFileName = "rows.csv", HasHeaderRecord = true, FullSourceSync = full,
                ProccessSourceData = rows =>
                {
                    var all = rows.ToList();
                    Assert.Equal(["FIRST", "SECOND"], all.Select(x => x.Id));
                    return new(all);
                },
            });
        engine.SetupMapping((rows, _) => new(rows));
        var stored = new List<string>();
        engine.SetupStoreBatchData(input =>
        {
            var rows = input.Input.Items.ToList();
            stored.AddRange(rows.Select(x => x.Id));
            return new(new SyncStoreDataResult<Row> { SucceededItems = rows });
        });
        Assert.True(await engine.RunAsync());
        Assert.Equal(["FIRST", "SECOND"], stored);
    }
}
