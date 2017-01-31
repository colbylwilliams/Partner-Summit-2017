﻿namespace XWeather.WeatherBot
{
	public static partial class Constants
	{
		public static class Endpoints
		{
			public const string AuthApi = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";

			public const string LUISApi = "https://api.projectoxford.ai/luis/v2.0/apps/";

			public const string BingSpeechApi = "https://speech.platform.bing.com/recognize";
		}
	}
}