using System;
using System.IO;
using System.Net;
using System.Text;
using System.Diagnostics;

namespace XWeather.WeatherBot
{
	public class Authentication
	{
		readonly string subscriptionId;

		public string Token { get; private set; }


		public Authentication (string subscriptionId)
		{
			this.subscriptionId = subscriptionId;
		}


		public void Init ()
		{
			if (string.IsNullOrEmpty (Token))
			{
				Token = HttpPost (Constants.Endpoints.AuthApi);
			}
		}


		public void RenewAccessToken ()
		{
			Token = null;
			Token = HttpPost (Constants.Endpoints.AuthApi);

			Debug.WriteLine (string.Format ("Renewed token for user: {0} is: {1}",
											subscriptionId,
											Token));
		}


		string HttpPost (string accessUri)
		{
			try
			{
				//Prepare auth request 
				var webRequest = WebRequest.Create (accessUri);
				webRequest.ContentType = "application/x-www-form-urlencoded";
				webRequest.Method = "POST";
				webRequest.ContentLength = 0;

				webRequest.Headers.Add ("Ocp-Apim-Subscription-Key", subscriptionId);

				using (WebResponse webResponse = webRequest.GetResponse ())
				using (Stream stream = webResponse.GetResponseStream ())
				{
					var reader = new StreamReader (stream, Encoding.UTF8);
					return reader.ReadToEnd ();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error during auth post: {0}", ex.Message);
				throw;
			}
		}
	}
}