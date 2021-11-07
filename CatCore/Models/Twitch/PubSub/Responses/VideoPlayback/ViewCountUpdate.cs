﻿namespace CatCore.Models.Twitch.PubSub.Responses.VideoPlayback
{
	public sealed class ViewCountUpdate : VideoPlaybackBase
	{
		public uint Viewers { get; }

		public ViewCountUpdate(string serverTimeRaw, uint viewers) : base(serverTimeRaw)
		{
			Viewers = viewers;
		}
	}
}