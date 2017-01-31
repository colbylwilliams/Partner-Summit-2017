namespace XWeather.WeatherBot
{
	public static partial class Constants
	{
		public static partial class LUIS
		{
			public static partial class Entities
			{
				public static class DateTime
				{
					public const string Current = "PRESENT_REF";

					public const string TimeRange = "builtin.weather.time_range";

					public const string DateRange = "builtin.weather.date_range";

					public const string DurationResolution = "builtin.datetime.duration";
				}
			}
		}
	}
}