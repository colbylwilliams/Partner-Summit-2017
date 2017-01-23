namespace XWeather.WeatherBot
{
	public class LUISResult
	{
		public string query { get; set; }

		public Intent [] intents { get; set; }

		public Entity [] entities { get; set; }
	}
}