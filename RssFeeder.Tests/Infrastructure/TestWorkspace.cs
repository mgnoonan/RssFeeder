namespace RssFeeder.Tests.Infrastructure;

internal sealed class TestWorkspace : IDisposable
{
    public TestWorkspace()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "RssFeeder.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public string CreateFile(string relativePath, string content)
    {
        var filePath = Path.Combine(RootPath, relativePath);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, content);
        return filePath;
    }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}