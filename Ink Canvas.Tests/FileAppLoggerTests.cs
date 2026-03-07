using Ink_Canvas.Services.Logging;
using System.Text;
using Xunit;

namespace Ink_Canvas.Tests;

public sealed class FileAppLoggerTests : IDisposable
{
    private readonly string testRoot = Path.Combine(Path.GetTempPath(), "InkCanvasLoggerTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void EnabledLogger_WritesCategorizedLine()
    {
        FileAppLogger logger = CreateLogger();

        logger.ForCategory("Unit").Info("hello world");

        string content = File.ReadAllText(GetActiveLogPath());
        Assert.Contains("[Info] [Unit] hello world", content);
    }

    [Fact]
    public void DisabledLogger_SkipsRegularWrites_AndCanBeReenabled()
    {
        FileAppLogger logger = CreateLogger();
        logger.SetEnabled(false);

        logger.Info("skip me");
        Assert.False(File.Exists(GetActiveLogPath()));

        logger.SetEnabled(true);
        logger.Info("write me");

        string content = File.ReadAllText(GetActiveLogPath());
        Assert.Contains("write me", content);
    }

    [Fact]
    public void ForcedError_WritesEvenWhenDisabled()
    {
        FileAppLogger logger = CreateLogger();
        logger.SetEnabled(false);

        logger.Error(new InvalidOperationException("boom"), "forced", force: true);

        string content = File.ReadAllText(GetActiveLogPath());
        Assert.Contains("[Error]", content);
        Assert.Contains("forced", content);
        Assert.Contains("boom", content);
    }

    [Fact]
    public void Logger_RotatesFiles_AndRetainsFiveArchives()
    {
        FileAppLogger logger = CreateLogger(maxFileSizeBytes: 120, retainedArchiveCount: 5);

        for (int index = 0; index < 20; index++)
        {
            logger.Info($"rotation message {index} {new string('x', 40)}");
            Thread.Sleep(5);
        }

        string[] archives = Directory.GetFiles(testRoot, "Log.*.txt");
        Assert.True(File.Exists(GetActiveLogPath()));
        Assert.InRange(archives.Length, 1, 5);
    }

    [Fact]
    public async Task Logger_SerializesConcurrentWrites()
    {
        FileAppLogger logger = CreateLogger(maxFileSizeBytes: 1024 * 1024);

        Task[] tasks = Enumerable.Range(0, 20)
            .Select(taskIndex => Task.Run(() =>
            {
                for (int lineIndex = 0; lineIndex < 25; lineIndex++)
                {
                    logger.ForCategory("Concurrent").Info($"task-{taskIndex}-line-{lineIndex}");
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        string[] lines = File.ReadAllLines(GetActiveLogPath(), Encoding.UTF8);
        Assert.Equal(500, lines.Length);
        Assert.All(lines, line => Assert.Contains("[Concurrent]", line));
    }

    [Fact]
    public void InvalidPath_DoesNotThrow()
    {
        FileAppLogger logger = new(new LogOptions
        {
            DirectoryPath = Path.Combine(testRoot, "bad\0path"),
            ActiveFileName = "Log.txt",
            Enabled = true,
            MaxFileSizeBytes = 1024,
            RetainedArchiveCount = 5
        });

        Exception? exception = Record.Exception(() => logger.Info("no throw"));
        Assert.Null(exception);
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    private FileAppLogger CreateLogger(long maxFileSizeBytes = 1024, int retainedArchiveCount = 5)
    {
        Directory.CreateDirectory(testRoot);
        return new FileAppLogger(new LogOptions
        {
            DirectoryPath = testRoot,
            ActiveFileName = "Log.txt",
            Enabled = true,
            MaxFileSizeBytes = maxFileSizeBytes,
            RetainedArchiveCount = retainedArchiveCount
        });
    }

    private string GetActiveLogPath() => Path.Combine(testRoot, "Log.txt");
}
