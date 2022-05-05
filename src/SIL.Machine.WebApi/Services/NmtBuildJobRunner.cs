namespace SIL.Machine.WebApi.Services;

public class NmtBuildJobRunner : INmtBuildJobRunner
{
	private readonly MongoUrl _mongoConnectionString;
	private readonly IOptions<TranslationEngineOptions> _translationEngineOptions;
	private readonly IOptions<CorpusOptions> _corpusOptions;

	public NmtBuildJobRunner(IConfiguration configuration, IOptions<TranslationEngineOptions> translationEngineOptions,
		IOptions<CorpusOptions> corpusOptions)
	{
		string connectionString = configuration.GetConnectionString("Mongo");
		_mongoConnectionString = new MongoUrl(connectionString);
		_translationEngineOptions = translationEngineOptions;
		_corpusOptions = corpusOptions;
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
			$"--engines-dir={_translationEngineOptions.Value.EnginesDir}",
			$"--data-files-dir={_corpusOptions.Value.DataFilesDir}",
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
