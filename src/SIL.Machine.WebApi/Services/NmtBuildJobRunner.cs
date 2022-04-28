namespace SIL.Machine.WebApi.Services;

public class NmtBuildJobRunner : INmtBuildJobRunner
{
	private readonly MongoUrl _mongoConnectionString;
	private readonly IOptions<EngineOptions> _engineOptions;

	public NmtBuildJobRunner(IConfiguration configuration, IOptions<EngineOptions> engineOptions)
	{
		string connectionString = configuration.GetConnectionString("Mongo");
		_mongoConnectionString = new MongoUrl(connectionString);
		_engineOptions = engineOptions;
	}

	public async Task RunAsync(string engineId, string buildId, CancellationToken cancellationToken = default)
	{
		string cancellationTokenFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ct");
		var args = new List<string>
		{
			"-m", "machine.webapi.nmt_engine_build_job",
			$"--engine={engineId}",
			$"--build={buildId}",
			$"--mongo={_mongoConnectionString.Server}",
			$"--database={_mongoConnectionString.DatabaseName}",
			$"--data-files-dir={_engineOptions.Value.DataFilesDir}",
			$"--cancellation-token-file={cancellationTokenFile}",
			$"--mixed-precision"
		};

		using var process = new Process();
		process.StartInfo.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "python.exe" : "python3";
		process.StartInfo.Arguments = string.Join(" ", args);
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.Start();

		try
		{
			await process.WaitForExitAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			await File.WriteAllTextAsync(cancellationTokenFile, null, CancellationToken.None);
			await process.WaitForExitAsync(CancellationToken.None);
			throw;
		}
		finally
		{
			if (File.Exists(cancellationTokenFile))
				File.Delete(cancellationTokenFile);
		}

		if (process.ExitCode != 0)
		{
			string errorMessage = await process.StandardError.ReadToEndAsync();
			throw new InvalidOperationException(errorMessage.Trim());
		}
	}
}
