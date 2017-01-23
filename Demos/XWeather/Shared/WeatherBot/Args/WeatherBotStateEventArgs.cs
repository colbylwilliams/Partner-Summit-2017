using System;

namespace XWeather.WeatherBot
{
	public class WeatherBotStateEventArgs : EventArgs
	{
		public WeatherBotState State { get; private set; }

		public string Message { get; private set; }

		public WeatherBotStateEventArgs (WeatherBotState state, string message) : this (state)
		{
			Message = message;
		}


		public WeatherBotStateEventArgs (WeatherBotState state)
		{
			State = state;
		}
	}
}