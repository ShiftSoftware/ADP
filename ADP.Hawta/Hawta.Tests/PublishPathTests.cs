using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// The published tier may live on a local folder, an SMB share, or a blob container reached by
/// URI. <see cref="Path"/> handles the first two and silently corrupts the third — these pin
/// the difference, because every one of the failures is quiet rather than loud.
/// </summary>
public class PublishPathTests
{
    [Theory]
    [InlineData("az://hawta/publish")]
    [InlineData("abfss://fs@account.dfs.core.windows.net/publish")]
    [InlineData("s3://bucket/publish")]
    [InlineData("https://account.blob.core.windows.net/hawta")]
    public void UriLocationsAreRemote(string location) => Assert.True(PublishPath.IsRemote(location));

    [Theory]
    [InlineData(@"C:\mounts\hawta\publish")]
    [InlineData(@"\\server\share\publish")]
    [InlineData("/mnt/hawta/publish")]
    [InlineData("publish")]
    [InlineData(@"C://mounts//hawta")]   // a drive root is not a scheme, however it is slashed
    public void LocalAndUncLocationsAreNotRemote(string location) => Assert.False(PublishPath.IsRemote(location));

    [Fact]
    public void CombineKeepsUrisForwardSlashed()
    {
        // Path.Combine injects a backslash here on Windows, producing "az://hawta/publish\x.parquet".
        Assert.Equal("az://hawta/publish/Widget-1.parquet",
            PublishPath.Combine("az://hawta/publish", "Widget-1.parquet"));

        // A trailing separator must not double up.
        Assert.Equal("az://hawta/publish/Widget-1.parquet",
            PublishPath.Combine("az://hawta/publish/", "Widget-1.parquet"));
    }

    [Fact]
    public void CombineStillUsesThePlatformSeparatorLocally() =>
        Assert.Equal(Path.Combine(@"C:\mounts\hawta", "Widget-1.parquet"),
            PublishPath.Combine(@"C:\mounts\hawta", "Widget-1.parquet"));

    [Fact]
    public void FileNameSplitsUrisOnForwardSlashesOnly() =>
        Assert.Equal("Widget-1.parquet", PublishPath.FileName("az://hawta/publish/Widget-1.parquet"));

    [Fact]
    public void DirectoryNameKeepsTheSchemeIntact()
    {
        // Path.GetDirectoryName collapses the scheme's "//" and backslashes the rest,
        // yielding "az:\hawta\publish".
        Assert.Equal("az://hawta/publish",
            PublishPath.DirectoryName("az://hawta/publish/Widget-1.parquet"));

        // A container root has no parent — never "az:/".
        Assert.Null(PublishPath.DirectoryName("az://hawta"));
    }

    [Theory]
    [InlineData("Widget-20260814120000000.parquet")]
    [InlineData("Widget/20260814120000000.parquet")]           // the folder-per-table layout
    [InlineData("Widget/_delta_log/00000000000000000000.json")] // and what Delta would need
    public void ContainedRelativePathsAreAccepted(string name) => Assert.True(PublishPath.IsRelativeContainedPath(name));

    [Theory]
    [InlineData("../escaped/Widget.parquet")]
    [InlineData("Widget/../../escaped.parquet")]
    [InlineData(@"..\escaped\Widget.parquet")]
    [InlineData(@"Widget\20260814120000000.parquet")]           // backslash is a file-name char on Linux, not a separator
    [InlineData(@"C:\elsewhere\Widget.parquet")]
    [InlineData("az://other/Widget.parquet")]
    [InlineData("/absolute/Widget.parquet")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    [InlineData("..")]
    public void AnythingEscapingTheManifestDirectoryIsRefused(string name) => Assert.False(PublishPath.IsRelativeContainedPath(name));

    [Fact]
    public void RequireLocalRefusesRemoteLocations()
    {
        // The point is that the alternative is silent: Directory.Exists("az://…") answers false
        // rather than throwing, and every caller reads that as "nothing is published yet".
        Assert.False(Directory.Exists("az://hawta/publish"));

        var thrown = Assert.Throws<NotSupportedException>(
            () => PublishPath.RequireLocal("az://hawta/publish", "Listing"));
        Assert.Contains("az://hawta/publish", thrown.Message);

        PublishPath.RequireLocal(Path.GetTempPath(), "Listing");   // does not throw
    }

    [Fact]
    public void GetFullPathIsTheHazardThisTypeExistsToAvoid()
    {
        // Documents the measured .NET behavior rather than trusting it: GetFullPath rebases the
        // URI under the current directory and returns a plausible-looking local path, with no
        // exception to notice. Nothing in the publish tier may call it.
        var mangled = Path.GetFullPath("az://hawta/publish");

        Assert.NotEqual("az://hawta/publish", mangled);
        Assert.DoesNotContain("az://", mangled);
    }
}
