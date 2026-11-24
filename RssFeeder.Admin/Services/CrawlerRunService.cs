using System.Diagnostics;

namespace RssFeeder.Admin.Services;

public sealed record CrawlerRunResult(int ExitCode, string Output);

public interface ICrawlerRunService
{
    Task<CrawlerRunResult> RunAuditAsync(CancellationToken cancellationToken = default);
}

public sealed class CrawlerRunService(IConfiguration configuration) : ICrawlerRunService
{
    private readonly SemaphoreSlim _runLock = new(1, 1);

    public async Task<CrawlerRunResult> RunAuditAsync(CancellationToken cancellationToken = default)
    {
        if (!await _runLock.WaitAsync(0, cancellationToken))
        {
            throw new InvalidOperationException("A crawler audit is already running.");
        }

        try
        {
            var projectPath = configuration["Crawler:ProjectPath"];
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                throw new InvalidOperationException(
                    "Crawler:ProjectPath is not configured. Run the Admin project through Aspire.");
            }

            if (!File.Exists(projectPath))
            {
                throw new FileNotFoundException("The configured crawler project was not found.", projectPath);
            }

            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = Path.GetDirectoryName(projectPath)!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add("audit");
            startInfo.ArgumentList.Add("feed-all.json");

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("The crawler process could not be started.");
            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                throw;
            }

            var standardOutput = await standardOutputTask;
            var standardError = await standardErrorTask;
            var output = string.Join(
                Environment.NewLine,
                new[] { standardOutput.Trim(), standardError.Trim() }.Where(value => value.Length > 0));

            return new CrawlerRunResult(process.ExitCode, output);
        }
        finally
        {
            _runLock.Release();
        }
    }
}
