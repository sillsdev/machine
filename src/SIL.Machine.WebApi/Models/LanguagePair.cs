using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SIL.Machine.WebApi.Services;
using SIL.ObjectModel;
using SIL.Threading;

namespace SIL.Machine.WebApi.Models
{
	public class LanguagePair : DisposableBase
	{
		private readonly AsyncLock _lock;
		private readonly KeyedList<string, Project> _projects;
		private readonly ISmtModelFactory _smtModelFactory;
		private readonly IRuleEngineFactory _ruleEngineFactory;
		private readonly ITextCorpusFactory _textCorpusFactory;
		private readonly TimeSpan _inactiveTimeout;
		private readonly string _trainProgressDir;
		private readonly string _configDir;
		private Engine _sharedEngine;

		public LanguagePair(ISmtModelFactory smtModelFactory, IRuleEngineFactory ruleEngineFactory, ITextCorpusFactory textCorpusFactory, TimeSpan inactiveTimeout,
			string trainProgressDir, string configDir)
			: this(smtModelFactory, ruleEngineFactory, textCorpusFactory, inactiveTimeout, trainProgressDir)
		{
			_configDir = configDir;
			string configFileName = Path.Combine(_configDir, "config.json");
			LanguagePairConfig config = JsonConvert.DeserializeObject<LanguagePairConfig>(File.ReadAllText(configFileName));
			SourceLanguageTag = config.SourceLanguageTag;
			TargetLanguageTag = config.TargetLanguageTag;
			foreach (ProjectConfig projectConfig in config.Projects)
			{
				CreateProject(projectConfig.Id, projectConfig.IsShared, out Project project);
				project.Engine.AddProject(project);
			}

			foreach (Engine engine in _projects.Select(p => p.Engine).Distinct())
				engine.InitExisting();
		}

		public LanguagePair(ISmtModelFactory smtModelFactory, IRuleEngineFactory ruleEngineFactory, ITextCorpusFactory textCorpusFactory, TimeSpan inactiveTimeout,
			string trainProgressDir, string rootDir, string sourceLanguageTag, string targetLanguageTag)
			: this(smtModelFactory, ruleEngineFactory, textCorpusFactory, inactiveTimeout, trainProgressDir)
		{
			_configDir = Path.Combine(rootDir, $"{sourceLanguageTag}_{targetLanguageTag}");
			if (!Directory.Exists(_configDir))
				Directory.CreateDirectory(_configDir);
			SourceLanguageTag = sourceLanguageTag;
			TargetLanguageTag = targetLanguageTag;
		}

		private LanguagePair(ISmtModelFactory smtModelFactory, IRuleEngineFactory ruleEngineFactory, ITextCorpusFactory textCorpusFactory, TimeSpan inactiveTimeout,
			string trainProgressDir)
		{
			_smtModelFactory = smtModelFactory;
			_ruleEngineFactory = ruleEngineFactory;
			_textCorpusFactory = textCorpusFactory;
			_inactiveTimeout = inactiveTimeout;
			_trainProgressDir = trainProgressDir;
			_lock = new AsyncLock();
			_projects = new KeyedList<string, Project>(p => p.Id);
		}

		public string SourceLanguageTag { get; }
		public string TargetLanguageTag { get; }

		public async Task<Project> GetProjectAsync(string projectId)
		{
			CheckDisposed();

			using (await _lock.WaitAsync())
			{
				if (_projects.TryGet(projectId, out Project p))
					return p;
				return null;
			}
		}

		public async Task<IReadOnlyCollection<Project>> GetProjectsAsync()
		{
			CheckDisposed();

			using (await _lock.WaitAsync())
			{
				return _projects.ToArray();
			}
		}

		public async Task<Project> AddProjectAsync(string projectId, bool isShared)
		{
			CheckDisposed();

			using (await _lock.WaitAsync())
			{
				if (!_projects.TryGet(projectId, out Project project))
				{
					if (CreateProject(projectId, isShared, out project))
						project.Engine.InitNew();
					await project.Engine.AddProjectAsync(project);
					await SaveConfigAsync();
				}
				return project;
			}
		}

		public async Task<bool> RemoveProjectAsync(string projectId)
		{
			CheckDisposed();

			using (await _lock.WaitAsync())
			{
				Project project;
				if (_projects.TryGet(projectId, out project))
				{
					_projects.Remove(projectId);
					if (await project.Engine.RemoveProjectAsync(project) && project.IsShared)
						_sharedEngine = null;
					project.Engine.Dispose();
					await SaveConfigAsync();
					return true;
				}

				return false;
			}
		}

		public async Task<IReadOnlyCollection<Engine>> GetEnginesAsync()
		{
			CheckDisposed();

			using (await _lock.WaitAsync())
				return _projects.Select(p => p.Engine).Distinct().ToArray();
		}

		public async Task<Engine> GetEngineAsync(string projectId)
		{
			CheckDisposed();

			using (await _lock.WaitAsync())
			{
				if (projectId == null)
					return _sharedEngine;

				if (_projects.TryGet(projectId, out Project project))
					return project.Engine;

				return null;
			}
		}

		public bool CreateProject(string id, bool isShared, out Project project)
		{
			CheckDisposed();

			bool isNew = false;
			string projectDir = Path.Combine(_configDir, id);
			Engine engine;
			if (isShared)
			{
				if (_sharedEngine == null)
				{
					_sharedEngine = CreateEngine(Path.Combine(_configDir, "shared-engine"));
					isNew = true;
				}
				engine = _sharedEngine;
			}
			else
			{
				engine = CreateEngine(projectDir);
				isNew = true;
			}

			project = new Project(id, isShared, projectDir, engine);
			_projects.Add(project);
			return isNew;
		}

		private Engine CreateEngine(string configDir)
		{
			return new Engine(_smtModelFactory, _ruleEngineFactory, _textCorpusFactory, _inactiveTimeout, _trainProgressDir, configDir,
				SourceLanguageTag, TargetLanguageTag);
		}

		private async Task SaveConfigAsync()
		{
			var config = new LanguagePairConfig
			{
				SourceLanguageTag = SourceLanguageTag,
				TargetLanguageTag = TargetLanguageTag,
				Projects = _projects.Select(p => new ProjectConfig {Id = p.Id, IsShared = p.IsShared}).ToArray()
			};
			using (var writer = new StreamWriter(File.OpenWrite(Path.Combine(_configDir, "config.json"))))
				await writer.WriteAsync(JsonConvert.SerializeObject(config, Formatting.Indented));
		}

		protected override void DisposeManagedResources()
		{
			foreach (Engine engine in _projects.Select(p => p.Engine).Distinct())
				engine.Dispose();
			_projects.Clear();
		}
	}
}
