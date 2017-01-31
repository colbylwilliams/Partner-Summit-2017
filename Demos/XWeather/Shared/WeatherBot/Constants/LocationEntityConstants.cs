namespace XWeather.WeatherBot
{
	public static partial class Constants
	{
		public static partial class LUIS
		{
			public static partial class Entities
			{
				public static class Location
				{
					public const string Absolute = "builtin.weather.absolute_location";
					public const string Implicit = "builtin.weather.implicit_location";

					public const string CurrentKey = "here";

					public static Entity Current =
						new Entity
						{
							type = Implicit,
							entity = CurrentKey
						};
				}
			}
		}
	}
}