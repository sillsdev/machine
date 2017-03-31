using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Options;
using SIL.ObjectModel;
using SIL.Threading;

namespace SIL.Machine.WebApi.Services
{
	public class EngineService : DisposableBase
	{
		private readonly EngineOptions _options;
		private readonly ConcurrentDictionary<(string SourceLanguageTag, string TargetLanguageTag), LanguagePair> _languagePairs;
		private readonly ISmtModelFactory _smtModelFactory;
		private readonly IRuleEngineFactory _ruleEngineFactory;
		private readonly ITextCorpusFactory _textCorpusFactory;
		private readonly StoppableTimer _commitTimer;

		public EngineService(IOptions<EngineOptions> options, ISmtModelFactory smtModelFactory, IRuleEngineFactory ruleEngineFactory, ITextCorpusFactory textCorpusFactory)
		{
			_options = options.Value;
			_smtModelFactory = smtModelFactory;
			_ruleEngineFactory = ruleEngineFactory;
			_textCorpusFactory = textCorpusFactory;
			_languagePairs = new ConcurrentDictionary<(string SourceLanguageTag, string TargetLanguageTag), LanguagePair>();
			foreach (string configDir in Directory.EnumerateDirectories(_options.RootDir))
			{
				string configFileName = Path.Combine(configDir, "config.json");
				if (!File.Exists(configFileName))
					continue;
				var languagePair = new LanguagePair(_smtModelFactory, _ruleEngineFactory, _textCorpusFactory, _options.InactiveEngineTimeout, _options.TrainProgressDir,
					configDir);
				// TODO: need to handle conflicting language pairs better
				if (!_languagePairs.TryAdd((languagePair.SourceLanguageTag, languagePair.TargetLanguageTag), languagePair))
					languagePair.Dispose();
			}
			_commitTimer = new StoppableTimer(EngineCommitAsync);
			_commitTimer.Start(_options.EngineCommitFrequency);
		}

		private async void EngineCommitAsync()
		{
			foreach (LanguagePair languagePair in _languagePairs.Values)
			{
				foreach (Engine engine in await languagePair.GetEnginesAsync())
				{
					try
					{
						await engine.CommitAsync();
					}
					catch (ObjectDisposedException)
					{
					}
				}
			}
		}

		public async Task<IReadOnlyCollection<LanguagePairDto>> GetLanguagePairsAsync()
		{
			var lps = new List<LanguagePairDto>();
			foreach (LanguagePair languagePair in _languagePairs.Values)
			{
				LanguagePairDto dto = await languagePair.CreateDtoAsync();
				if (dto.Projects.Count > 0)
					lps.Add(dto);
			}
			return lps;
		}

		public async Task<LanguagePairDto> GetLanguagePairAsync(string sourceLanguageTag, string targetLanguageTag)
		{
			if (_languagePairs.TryGetValue((sourceLanguageTag, targetLanguageTag), out LanguagePair languagePair))
			{
				LanguagePairDto dto = await languagePair.CreateDtoAsync();
				return dto.Projects.Count == 0 ? null : dto;
			}

			return null;
		}

		public async Task<IReadOnlyCollection<ProjectDto>> GetProjectsAsync(string sourceLanguageTag, string targetLanguageTag)
		{
			if (_languagePairs.TryGetValue((sourceLanguageTag, targetLanguageTag), out LanguagePair languagePair))
			{
				IReadOnlyCollection<Project> projects = await languagePair.GetProjectsAsync();
				return projects.Select(p => p.CreateDto()).ToArray();
			}

			return null;
		}

		public async Task<ProjectDto> GetProjectAsync(string sourceLanguageTag, string targetLanguageTag, string projectId)
		{
			if (_languagePairs.TryGetValue((sourceLanguageTag, targetLanguageTag), out LanguagePair languagePair))
			{
				Project project = await languagePair.GetProjectAsync(projectId);
				if (project != null)
					return project.CreateDto();
			}

			return null;
		}

		public async Task<ProjectDto> AddProjectAsync(string sourceLanguageTag, string targetLanguageTag, ProjectDto newProject)
		{
			LanguagePair languagePair = _languagePairs.GetOrAdd((sourceLanguageTag, targetLanguageTag),
				k => new LanguagePair(_smtModelFactory, _ruleEngineFactory, _textCorpusFactory, _options.InactiveEngineTimeout, _options.TrainProgressDir,
					_options.RootDir, sourceLanguageTag, targetLanguageTag));
			Project project = await languagePair.AddProjectAsync(newProject.Id, newProject.IsShared);
			return project.CreateDto();
		}

		public async Task<bool> RemoveProjectAsync(string sourceLanguageTag, string targetLanguageTag, string projectId)
		{
			if (_languagePairs.TryGetValue((sourceLanguageTag, targetLanguageTag), out LanguagePair languagePair))
				return await languagePair.RemoveProjectAsync(projectId);
			return false;
		}

		public async Task<TranslationResultDto> TranslateAsync(string sourceLanguageTag, string targetLanguageTag, string projectId, IReadOnlyList<string> segment)
		{
			Engine engine = await GetEngineAsync(sourceLanguageTag, targetLanguageTag, projectId);
			if (engine != null)
			{
				try
				{
					TranslationResult tr = await engine.TranslateAsync(segment.Select(w => w.ToLowerInvariant()).ToArray());
					return tr.CreateDto(segment);
				}
				catch (ObjectDisposedException)
				{
				}
			}

			return null;
		}

		public async Task<IReadOnlyList<TranslationResultDto>> TranslateAsync(string sourceLanguageTag, string targetLanguageTag, string projectId, int n, IReadOnlyList<string> segment)
		{
			Engine engine = await GetEngineAsync(sourceLanguageTag, targetLanguageTag, projectId);
			if (engine != null)
			{
				try
				{
					IReadOnlyList<TranslationResult> trs = await engine.TranslateAsync(n, segment.Select(w => w.ToLowerInvariant()).ToArray());
					return trs.Select(tr => tr.CreateDto(segment)).ToArray();
				}
				catch (ObjectDisposedException)
				{
				}
			}

			return null;
		}

		public async Task<InteractiveTranslationResultDto> InteractiveTranslateAsync(string sourceLanguageTag, string targetLanguageTag, string projectId, IReadOnlyList<string> segment)
		{
			Engine engine = await GetEngineAsync(sourceLanguageTag, targetLanguageTag, projectId);
			if (engine != null)
			{
				try
				{
					(WordGraph WordGraph, TranslationResult RuleResult) result = await engine.InteractiveTranslateAsync(segment.Select(s => s.ToLowerInvariant()).ToArray());
					return new InteractiveTranslationResultDto
					{
						WordGraph = result.WordGraph.CreateDto(segment),
						RuleResult = result.RuleResult?.CreateDto(segment)
					};
				}
				catch (ObjectDisposedException)
				{
				}
			}

			return null;
		}

		public async Task<bool> TrainSegmentAsync(string sourceLanguageTag, string targetLanguageTag, string projectId, SegmentPairDto segmentPair)
		{
			Engine engine = await GetEngineAsync(sourceLanguageTag, targetLanguageTag, projectId);
			if (engine != null)
			{
				try
				{
					await engine.TrainSegmentPairAsync(segmentPair.SourceSegment.Select(s => s.ToLowerInvariant()).ToArray(),
						segmentPair.TargetSegment.Select(s => s.ToLowerInvariant()).ToArray());
					return true;
				}
				catch (ObjectDisposedException)
				{
				}
			}

			return false;
		}

	    public async Task<bool> StartRebuildAsync(string sourceLanguageTag, string targetLanguageTag, string projectId)
	    {
		    Engine engine = await GetEngineAsync(sourceLanguageTag, targetLanguageTag, projectId);
		    if (engine != null)
		    {
			    try
			    {
				    await engine.StartRebuildAsync();
				    return true;
			    }
			    catch (ObjectDisposedException)
			    {
			    }
		    }

			return false;
		}

		public async Task<bool> CancelRebuildAsync(string sourceLanguageTag, string targetLanguageTag, string projectId)
		{
			Engine engine = await GetEngineAsync(sourceLanguageTag, targetLanguageTag, projectId);
			if (engine != null)
			{
				try
				{
					await engine.CancelRebuildAsync();
					return true;
				}
				catch (ObjectDisposedException)
				{
				}
			}

			return false;
		}

		private async Task<Engine> GetEngineAsync(string sourceLanguageTag, string targetLanguageTag, string projectId)
		{
			if (_languagePairs.TryGetValue((sourceLanguageTag, targetLanguageTag), out LanguagePair languagePair))
				return await languagePair.GetEngineAsync(projectId);

			return null;
		}

		protected override void DisposeManagedResources()
		{
			_commitTimer.Dispose();

			foreach (LanguagePair languagePair in _languagePairs.Values)
				languagePair.Dispose();

			_languagePairs.Clear();
		}
	}
}
