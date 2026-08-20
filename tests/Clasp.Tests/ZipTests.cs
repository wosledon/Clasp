using Clasp.Commands;
using Clasp.Plugin;

namespace Clasp.Tests;

public class ZipTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenConflictingModes()
    {
        var command = CommandTestHelper.CreateCommand<Zip>();
        CommandTestHelper.SetOption(command, nameof(Zip.Create), true);
        CommandTestHelper.SetOption(command, nameof(Zip.Extract), true);

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenArchiveNotExistsForList()
    {
        var command = CommandTestHelper.CreateCommand<Zip>();
        var args = new ClaspCommandArgs
        {
            Command = "zip"
        };
        args.AddValue("nonexistent.zip");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command, args);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_ForCreateMode()
    {
        var command = CommandTestHelper.CreateCommand<Zip>();
        CommandTestHelper.SetOption(command, nameof(Zip.Create), true);
        var args = new ClaspCommandArgs
        {
            Command = "zip"
        };
        args.AddValue("test.zip");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command, args);
        Assert.False(threw);
    }

    [Fact]
    public async Task CreateAndListZip_ShouldWork()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "clasp-zip-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var testFile = Path.Combine(tempDir, "hello.txt");
            await File.WriteAllTextAsync(testFile, "Hello, World!");

            var zipPath = Path.Combine(tempDir, "test.zip");

            // Create
            var createCmd = CommandTestHelper.CreateCommand<Zip>();
            CommandTestHelper.SetOption(createCmd, nameof(Zip.Create), true);
            var createArgs = new ClaspCommandArgs { Command = "zip" };
            createArgs.AddValue(zipPath);
            createArgs.AddValue(testFile);
            await createCmd.ExecuteAsync(createArgs, CancellationToken.None);

            Assert.True(File.Exists(zipPath));

            // List
            var listCmd = CommandTestHelper.CreateCommand<Zip>();
            var listArgs = new ClaspCommandArgs { Command = "zip" };
            listArgs.AddValue(zipPath);
            await listCmd.ExecuteAsync(listArgs, CancellationToken.None);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExtractZip_ShouldWork()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "clasp-zip-extract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var testFile = Path.Combine(tempDir, "hello.txt");
            await File.WriteAllTextAsync(testFile, "Hello, World!");

            var zipPath = Path.Combine(tempDir, "test.zip");
            var extractDir = Path.Combine(tempDir, "out");

            // Create
            var createCmd = CommandTestHelper.CreateCommand<Zip>();
            CommandTestHelper.SetOption(createCmd, nameof(Zip.Create), true);
            var createArgs = new ClaspCommandArgs { Command = "zip" };
            createArgs.AddValue(zipPath);
            createArgs.AddValue(testFile);
            await createCmd.ExecuteAsync(createArgs, CancellationToken.None);

            // Extract
            var extractCmd = CommandTestHelper.CreateCommand<Zip>();
            CommandTestHelper.SetOption(extractCmd, nameof(Zip.Extract), true);
            CommandTestHelper.SetOption(extractCmd, nameof(Zip.OutputDir), extractDir);
            var extractArgs = new ClaspCommandArgs { Command = "zip" };
            extractArgs.AddValue(zipPath);
            await extractCmd.ExecuteAsync(extractArgs, CancellationToken.None);

            var extractedFile = Path.Combine(extractDir, "hello.txt");
            Assert.True(File.Exists(extractedFile));
            Assert.Equal("Hello, World!", await File.ReadAllTextAsync(extractedFile));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAndListTar_ShouldWork()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "clasp-tar-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var testFile = Path.Combine(tempDir, "hello.txt");
            await File.WriteAllTextAsync(testFile, "Hello, World!");

            var tarPath = Path.Combine(tempDir, "test.tar");

            // Create
            var createCmd = CommandTestHelper.CreateCommand<Zip>();
            CommandTestHelper.SetOption(createCmd, nameof(Zip.Create), true);
            var createArgs = new ClaspCommandArgs { Command = "zip" };
            createArgs.AddValue(tarPath);
            createArgs.AddValue(testFile);
            await createCmd.ExecuteAsync(createArgs, CancellationToken.None);

            Assert.True(File.Exists(tarPath));

            // List
            var listCmd = CommandTestHelper.CreateCommand<Zip>();
            var listArgs = new ClaspCommandArgs { Command = "zip" };
            listArgs.AddValue(tarPath);
            await listCmd.ExecuteAsync(listArgs, CancellationToken.None);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAndExtractTarGz_ShouldWork()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "clasp-tgz-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var testFile = Path.Combine(tempDir, "hello.txt");
            await File.WriteAllTextAsync(testFile, "Hello, World!");

            var tgzPath = Path.Combine(tempDir, "test.tar.gz");
            var extractDir = Path.Combine(tempDir, "out");

            // Create
            var createCmd = CommandTestHelper.CreateCommand<Zip>();
            CommandTestHelper.SetOption(createCmd, nameof(Zip.Create), true);
            var createArgs = new ClaspCommandArgs { Command = "zip" };
            createArgs.AddValue(tgzPath);
            createArgs.AddValue(testFile);
            await createCmd.ExecuteAsync(createArgs, CancellationToken.None);

            Assert.True(File.Exists(tgzPath));

            // Extract
            var extractCmd = CommandTestHelper.CreateCommand<Zip>();
            CommandTestHelper.SetOption(extractCmd, nameof(Zip.Extract), true);
            CommandTestHelper.SetOption(extractCmd, nameof(Zip.OutputDir), extractDir);
            var extractArgs = new ClaspCommandArgs { Command = "zip" };
            extractArgs.AddValue(tgzPath);
            await extractCmd.ExecuteAsync(extractArgs, CancellationToken.None);

            var extractedFile = Path.Combine(extractDir, "hello.txt");
            Assert.True(File.Exists(extractedFile));
            Assert.Equal("Hello, World!", await File.ReadAllTextAsync(extractedFile));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}