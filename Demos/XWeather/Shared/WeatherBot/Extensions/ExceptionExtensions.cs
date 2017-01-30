using System;
using System.Net;

namespace XWeather.WeatherBot
{
	public static class ExceptionExtensions
	{
		public static bool CheckWebResponseStatus (this Exception ex, HttpStatusCode code)
		{
			if (ex is WebException)
			{
				var webEx = (WebException)ex;

				if (webEx.Response is HttpWebResponse)
				{
					return ((HttpWebResponse)webEx.Response).StatusCode == code;
				}
			}

			return false;
		}
	}
}