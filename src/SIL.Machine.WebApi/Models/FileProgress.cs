using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SIL.Machine.Translation;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Models
{
	public class FileProgress : DisposableBase, IProgress<SmtTrainProgress>
	{
		private readonly string _path;
		private readonly JsonSerializerSettings _settings;
		private readonly Engine _engine;
		private readonly HashSet<Project> _projects;

		public FileProgress(string path, Engine engine)
		{
			_path = path;
			_engine = engine;
			_projects = new HashSet<Project>();
			_settings = new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
				Formatting = Formatting.Indented
			};
			if (!Directory.Exists(_path))
				Directory.CreateDirectory(_path);
		}

		public void Report(SmtTrainProgress value)
		{
			IReadOnlyCollection<Project> projects = _engine.GetProjects();

			foreach (Project project in projects)
			{
				string json = JsonConvert.SerializeObject(value, _settings);
				File.WriteAllText(Path.Combine(_path, project.Id + ".json"), json);
			}

			_projects.UnionWith(projects);
		}

		protected override void DisposeManagedResources()
		{
			foreach (Project project in _projects)
				File.Delete(Path.Combine(_path, project.Id + ".json"));
		}
	}
}
