using System;

namespace XWeather.WeatherBot
{
	public class WeatherBotRequestEventArgs : EventArgs
	{
		public Entity Location { get; private set; }

		public DateTime? RequestDateTime { get; private set; }

		public bool UseCurrentLocation { get; private set; }


		public WeatherBotRequestEventArgs (Entity location, DateTime? requestDateTime) : this (location)
		{
			RequestDateTime = requestDateTime;
		}


		public WeatherBotRequestEventArgs (Entity location)
		{
			Location = location;

			UseCurrentLocation = location.IsCurrentLocation ();
		}
	}
}