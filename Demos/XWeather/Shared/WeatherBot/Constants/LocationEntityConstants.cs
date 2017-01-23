namespace XWeather.WeatherBot
{
	public static class LocationEntityConstants
	{
		public const string AbsoluteLocationEntity = "builtin.weather.absolute_location";
		public const string ImplicitLocationEntity = "builtin.weather.implicit_location";

		public const string CurrentLocation = "here";

		public static Entity CurrentLocationEntity =
			new Entity
			{
				type = ImplicitLocationEntity,
				entity = CurrentLocation
			};
	}
}