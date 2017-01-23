using System;

namespace XWeather.WeatherBot
{
	public class Resolution
	{
		public string metadataType { get; set; }

		public string resolution_type { get; set; }

		public string value { get; set; }

		public DateTime? date { get; set; }

		public string time { get; set; }

		public string duration { get; set; }
	}
}