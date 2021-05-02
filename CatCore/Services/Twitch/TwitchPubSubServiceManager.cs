﻿using System.Collections.Generic;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Models.Shared;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal class TwitchPubSubServiceManager : ITwitchPubSubServiceManager
	{
		private readonly ILogger _logger;
		private readonly ThreadSafeRandomFactory _randomFactory;
		private readonly IKittenPlatformActiveStateManager _activeStateManager;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchChannelManagementService _twitchChannelManagementService;

		private readonly Dictionary<string, TwitchPubSubServiceAgent> _activePubSubConnections;

		public TwitchPubSubServiceManager(ILogger logger, ThreadSafeRandomFactory randomFactory, IKittenPlatformActiveStateManager activeStateManager, ITwitchAuthService twitchAuthService,
			ITwitchChannelManagementService twitchChannelManagementService)
		{
			_logger = logger;
			_randomFactory = randomFactory;
			_activeStateManager = activeStateManager;
			_twitchAuthService = twitchAuthService;
			_twitchChannelManagementService = twitchChannelManagementService;

			_twitchAuthService.OnCredentialsChanged += TwitchAuthServiceOnOnCredentialsChanged;

			_activePubSubConnections = new Dictionary<string, TwitchPubSubServiceAgent>();
		}

		async Task ITwitchPubSubServiceManager.Start()
		{
			foreach (var channelId in _twitchChannelManagementService.GetAllActiveChannelIds())
			{
				var agent = new TwitchPubSubServiceAgent(_logger, _randomFactory.CreateNewRandom(), _twitchAuthService, channelId);
				await agent.Start().ConfigureAwait(false);

				_activePubSubConnections[channelId] = agent;
			}
		}

		async Task ITwitchPubSubServiceManager.Stop()
		{
			foreach (var twitchPubSubServiceAgent in _activePubSubConnections)
			{
				await twitchPubSubServiceAgent.Value.Stop().ConfigureAwait(false);
			}

			_activePubSubConnections.Clear();
		}

		private async void TwitchAuthServiceOnOnCredentialsChanged()
		{
			if (_twitchAuthService.HasTokens)
			{
				if (_activeStateManager.GetState(PlatformType.Twitch))
				{
					foreach (var twitchPubSubServiceAgent in _activePubSubConnections)
					{
						await twitchPubSubServiceAgent.Value.Start().ConfigureAwait(false);
					}
				}
			}
			else
			{
				foreach (var twitchPubSubServiceAgent in _activePubSubConnections)
				{
					await twitchPubSubServiceAgent.Value.Stop().ConfigureAwait(false);
				}
			}
		}
	}
}