namespace XWeather.Constants
{
	public static class PrivateKeys
	{
		public const string WuApiKey = @"";

		public const string GoogleMapsApiKey = @"";

		public static class MobileCenter
		{
#if __IOS__
			public const string AppSecret = @"";

			public const string ServiceUrl = @"";
#elif __ANDROID__
			public const string AppSecret = @"";

			public const string ServiceUrl = @"";
#endif
		}

		public static class CognitiveServices
		{
			public const string BingSpeech = @"";

			public const string Luis = @"";
		}
	}
}