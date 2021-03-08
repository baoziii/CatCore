﻿using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using CatCore.Helpers;
using CatCore.Models.Config;
using CatCore.Services.Interfaces;
using Serilog;

namespace CatCore.Services
{
	internal class KittenSettingsService : IKittenSettingsService
	{
		private const string CONFIG_FILENAME = nameof(CatCore) + "Settings.json";

		private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

		private readonly ILogger _logger;
		private readonly IKittenPathProvider _pathProvider;
		private readonly string _credentialsFilePath;

		private readonly JsonSerializerOptions _jsonSerializerOptions;

		private readonly FileSystemWatcher _fileSystemWatcher;

		public ConfigRoot Config { get; private set; } = null!;

		public KittenSettingsService(ILogger logger, IKittenPathProvider pathProvider)
		{
			_logger = logger;
			_pathProvider = pathProvider;
			_credentialsFilePath = Path.Combine(_pathProvider.DataPath, CONFIG_FILENAME);

			_jsonSerializerOptions = new JsonSerializerOptions {WriteIndented = true};

			_fileSystemWatcher = new FileSystemWatcher
			{
				Path = pathProvider.DataPath,
				NotifyFilter = NotifyFilters.LastWrite,
				Filter = CONFIG_FILENAME,
				EnableRaisingEvents = true,
				IncludeSubdirectories = false
			};
			_fileSystemWatcher.Changed += OnFileSystemChanged;
		}

		public void Initialize()
		{
			Load();
			Store();
		}

		public void Load()
		{
			try
			{
				_locker.Wait();

				_logger.Information("Loading {Name} settings", nameof(CatCore));

				if (!Directory.Exists(_pathProvider.DataPath))
				{
					Directory.CreateDirectory(_pathProvider.DataPath);
				}

				if (!File.Exists(_credentialsFilePath))
				{
					Config = new ConfigRoot();
					return;
				}

				var readAllText = File.ReadAllText(_credentialsFilePath);
				Config = JsonSerializer.Deserialize<ConfigRoot>(readAllText, _jsonSerializerOptions) ?? new ConfigRoot();
			}
			catch (Exception e)
			{
				_logger.Error(e, "An error occurred while trying to load the {Name} settings", nameof(CatCore));
				Config = new ConfigRoot();
			}
			finally
			{
				_locker.Release();
			}
		}

		public void Store()
		{
			try
			{
				_locker.Wait();

				_logger.Information("Storing {Name} settings", nameof(CatCore));

				if (!Directory.Exists(_pathProvider.DataPath))
				{
					Directory.CreateDirectory(_pathProvider.DataPath);
				}

				File.WriteAllText(_credentialsFilePath, JsonSerializer.Serialize(Config, _jsonSerializerOptions));
			}
			catch (Exception e)
			{
				_logger.Error(e, "An error occurred while trying to store the {Name} settings", nameof(CatCore));
			}
			finally
			{
				_locker.Release();
			}
		}

		public IDisposable ChangeTransaction()
		{
			return WeakActionToken.Create(this, provider => provider.Store());
		}

		private void OnFileSystemChanged(object sender, FileSystemEventArgs args)
		{
		}

		~KittenSettingsService()
		{
			_fileSystemWatcher.Changed -= OnFileSystemChanged;
		}
	}
}