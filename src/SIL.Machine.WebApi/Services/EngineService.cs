using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Services
{
	public class EngineService : DisposableBase
	{
		private readonly EngineOptions _options;
		private readonly ConcurrentDictionary<Tuple<string, string>, LanguagePair> _languagePairs;
		private readonly ISmtModelFactory _smtModelFactory;
		private readonly ITranslationEngineFactory _ruleEngineFactory;
		private readonly Timer _commitTimer;
		private bool _isCommitTimerStopped;

		public EngineService(IOptions<EngineOptions> options, ISmtModelFactory smtModelFactory, ITranslationEngineFactory ruleEngineFactory)
		{
			_options = options.Value;
			_smtModelFactory = smtModelFactory;
			_ruleEngineFactory = ruleEngineFactory;
			_languagePairs = new ConcurrentDictionary<Tuple<string, string>, LanguagePair>();
			foreach (string configDir in Directory.EnumerateDirectories(_options.RootDir))
			{
				string configFileName = Path.Combine(configDir, "config.json");
				if (!File.Exists(configFileName))
					continue;
				LanguagePairDto languagePairConfig = JsonConvert.DeserializeObject<LanguagePairDto>(File.ReadAllText(configFileName));
				var languagePair = new LanguagePair(languagePairConfig.SourceLanguageTag, languagePairConfig.TargetLanguageTag, configDir);
				foreach (ProjectDto projectConfig in languagePairConfig.Projects)
					CreateProject(languagePair, projectConfig.Id, projectConfig.IsShared);
				_languagePairs[Tuple.Create(languagePair.SourceLanguageTag, languagePair.TargetLanguageTag)] = languagePair;
			}
			_commitTimer = new Timer(EngineCommitCallback, null, _options.EngineCommitFrequency, _options.EngineCommitFrequency);
		}

		private void EngineCommitCallback(object state)
		{
			if (_isCommitTimerStopped)
				return;

			foreach (LanguagePair languagePair in _languagePairs.Values)
			{
				Engine[] engines;
				lock (languagePair)
				{
					engines = languagePair.Projects.Select(p => p.Engine).Distinct().ToArray();
				}

				foreach (Engine engine in engines)
				{
					lock (engine)
					{
						if (engine.IsDisposed)
							continue;

						engine.Commit();
					}
				}
			}
		}

		public IEnumerable<LanguagePairDto> GetAllLanguagePairs()
		{
			foreach (LanguagePair languagePair in _languagePairs.Values)
			{
				lock (languagePair)
					yield return languagePair.CreateDto();
			}
		}

		public bool TryGetLanguagePair(string sourceLanguageTag, string targetLanguageTag, out LanguagePairDto languagePair)
		{
			LanguagePair lp;
			if (_languagePairs.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out lp))
			{
				lock (lp)
				{
					if (lp.Projects.Count > 0)
					{
						languagePair = lp.CreateDto();
						return true;
					}
				}
			}

			languagePair = null;
			return false;
		}

		public bool GetAllProjects(string sourceLanguageTag, string targetLanguageTag, out IReadOnlyList<ProjectDto> projects)
		{
			LanguagePair languagePair;
			if (_languagePairs.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out languagePair))
			{
				lock (languagePair)
				{
					if (languagePair.Projects.Count > 0)
					{
						projects = languagePair.Projects.Select(p => p.CreateDto()).ToArray();
						return true;
					}
				}
			}

			projects = null;
			return false;
		}

		public bool TryGetProject(string sourceLanguageTag, string targetLanguageTag, string projectId, out ProjectDto project)
		{
			LanguagePair languagePair;
			if (_languagePairs.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out languagePair))
			{
				lock (languagePair)
				{
					Project p;
					if (languagePair.Projects.TryGet(projectId, out p))
					{
						project = p.CreateDto();
						return true;
					}
				}
			}

			project = null;
			return false;
		}

		public ProjectDto AddProject(string sourceLanguageTag, string targetLanguageTag, ProjectDto newProject)
		{
			LanguagePair languagePair = _languagePairs.GetOrAdd(Tuple.Create(sourceLanguageTag, targetLanguageTag),
				k => new LanguagePair(sourceLanguageTag, targetLanguageTag, Path.Combine(_options.RootDir, $"{sourceLanguageTag}_{targetLanguageTag}")));
			lock (languagePair)
			{
				if (!Directory.Exists(languagePair.ConfigDirectory))
					Directory.CreateDirectory(languagePair.ConfigDirectory);
				Project project = CreateProject(languagePair, newProject.Id, newProject.IsShared);
				SaveLanguagePairConfig(languagePair);
				return project.CreateDto();
			}
		}

		public bool RemoveProject(string sourceLanguageTag, string targetLanguageTag, string projectId)
		{
			LanguagePair languagePair;
			if (_languagePairs.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out languagePair))
			{
				lock (languagePair)
				{
					Project project;
					if (languagePair.Projects.TryGet(projectId, out project))
					{
						languagePair.Projects.Remove(projectId);
						lock (project.Engine)
						{
							project.Engine.Projects.Remove(project);
							if (project.Engine.Projects.Count == 0)
							{
								project.Engine.Dispose();
								if (project.Engine == languagePair.SharedEngine)
									languagePair.SharedEngine = null;
								if (Directory.Exists(project.Engine.ConfigDirectory))
									Directory.Delete(project.Engine.ConfigDirectory, true);
							}
						}

						SaveLanguagePairConfig(languagePair);
						return true;
					}
				}
			}
			return false;
		}

		public bool TryTranslate(string sourceLanguageTag, string targetLanguageTag, string projectId, IReadOnlyList<string> segment, out IReadOnlyList<string> result)
		{
			Engine engine;
			if (TryGetEngine(sourceLanguageTag, targetLanguageTag, projectId, out engine))
			{
				lock (engine)
				{
					if (engine.IsDisposed)
					{
						result = null;
						return false;
					}

					TranslationResult tr = engine.Translate(segment.Select(w => w.ToLowerInvariant()).ToArray());
					result = Enumerable.Range(0, tr.TargetSegment.Count).Select(j => tr.RecaseTargetWord(segment, j)).ToArray();
					return true;
				}
			}

			result = null;
			return false;
		}

		public bool TryInteractiveTranslate(string sourceLanguageTag, string targetLanguageTag, string projectId, IReadOnlyList<string> segment, out InteractiveTranslationResultDto result)
		{
			Engine engine;
			if (TryGetEngine(sourceLanguageTag, targetLanguageTag, projectId, out engine))
			{
				lock (engine)
				{
					if (engine.IsDisposed)
					{
						result = null;
						return false;
					}

					WordGraph smtWordGraph;
					TranslationResult ruleResult;
					engine.InteractiveTranslate(segment.Select(s => s.ToLowerInvariant()).ToArray(), out smtWordGraph, out ruleResult);
					result = new InteractiveTranslationResultDto
					{
						WordGraph = smtWordGraph.CreateDto(segment),
						RuleResult = ruleResult?.CreateDto(segment)
					};
					return true;
				}
			}

			result = null;
			return false;
		}

		public bool TryTrainSegment(string sourceLanguageTag, string targetLanguageTag, string projectId, SegmentPairDto segmentPair)
		{
			Engine engine;
			if (TryGetEngine(sourceLanguageTag, targetLanguageTag, projectId, out engine))
			{
				lock (engine)
				{
					if (engine.IsDisposed)
						return false;

					engine.TrainSegmentPair(segmentPair.SourceSegment.Select(s => s.ToLowerInvariant()).ToArray(),
						segmentPair.TargetSegment.Select(s => s.ToLowerInvariant()).ToArray());
					return true;
				}
			}

			return false;
		}

		private bool TryGetEngine(string sourceLanguageTag, string targetLanguageTag, string projectId, out Engine engine)
		{
			LanguagePair languagePair;
			if (_languagePairs.TryGetValue(Tuple.Create(sourceLanguageTag, targetLanguageTag), out languagePair))
			{
				lock (languagePair)
				{
					if (projectId == null)
					{
						engine = languagePair.SharedEngine;
						return true;
					}

					Project project;
					if (languagePair.Projects.TryGet(projectId, out project))
					{
						engine = project.Engine;
						return true;
					}
				}
			}

			engine = null;
			return false;
		}

		private Project CreateProject(LanguagePair languagePair, string id, bool isShared)
		{
			Engine engine;
			if (isShared)
			{
				if (languagePair.SharedEngine == null)
					languagePair.SharedEngine = CreateEngine(languagePair, Path.Combine(languagePair.ConfigDirectory, "shared-engine"));
				engine = languagePair.SharedEngine;
			}
			else
			{
				string projectDir = Path.Combine(languagePair.ConfigDirectory, id);
				engine = CreateEngine(languagePair, projectDir);
			}

			var project = new Project(id, isShared, engine);
			languagePair.Projects.Add(project);
			engine.Projects.Add(project);
			return project;
		}

		private Engine CreateEngine(LanguagePair languagePair, string configDir)
		{
			return new Engine(_smtModelFactory, _ruleEngineFactory, _options.InactiveEngineTimeout, configDir,
				languagePair.SourceLanguageTag, languagePair.TargetLanguageTag);
		}

		private void SaveLanguagePairConfig(LanguagePair languagePair)
		{
			File.WriteAllText(Path.Combine(languagePair.ConfigDirectory, "config.json"), JsonConvert.SerializeObject(languagePair.CreateDto()));
		}

		protected override void DisposeManagedResources()
		{
			_isCommitTimerStopped = true;
			_commitTimer.Dispose();

			foreach (LanguagePair languagePair in _languagePairs.Values)
			{
				lock (languagePair)
				{
					foreach (Engine engineContext in languagePair.Projects.Select(p => p.Engine).Distinct())
					{
						lock (engineContext)
						{
							engineContext.Dispose();
						}
					}
				}
			}

			_languagePairs.Clear();
		}
	}
}
