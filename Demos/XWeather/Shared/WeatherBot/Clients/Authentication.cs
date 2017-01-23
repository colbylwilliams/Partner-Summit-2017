using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace XWeather.WeatherBot
{
	public class Authentication
	{
		public static readonly string AccessUri = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";

		readonly string subscriptionId;

		string jwtAuthToken;
		Timer accessTokenRenewer;

		// TODO: NO idea if this is valid / still needed now that auth is done via JWT tokens
		//Access token expires every 10 minutes. Renew it every 9 minutes only.
		const int RefreshTokenDuration = 9;

		public Authentication (string subscriptionId)
		{
			this.subscriptionId = subscriptionId;

			// renew the token every specfied minutes
			accessTokenRenewer = new Timer (new TimerCallback (OnTokenExpiredCallback),
										   this,
										   TimeSpan.FromMinutes (RefreshTokenDuration),
										   TimeSpan.FromMilliseconds (-1));
		}


		public void Init ()
		{
			if (string.IsNullOrEmpty (jwtAuthToken))
			{
				this.jwtAuthToken = HttpPost (AccessUri);
			}
		}


		public string GetAccessToken ()
		{
			return this.jwtAuthToken;
		}


		void RenewAccessToken ()
		{
			this.jwtAuthToken = HttpPost (AccessUri);

			Debug.WriteLine (string.Format ("Renewed token for user: {0} is: {1}",
											this.subscriptionId,
											this.jwtAuthToken));
		}


		void OnTokenExpiredCallback (object stateInfo)
		{
			try
			{
				RenewAccessToken ();
			}
			catch (Exception ex)
			{
				Debug.WriteLine (string.Format ("Failed renewing access token. Details: {0}", ex.Message));
			}
			finally
			{
				try
				{
					accessTokenRenewer.Change (TimeSpan.FromMinutes (RefreshTokenDuration), TimeSpan.FromMilliseconds (-1));
				}
				catch (Exception ex)
				{
					Debug.WriteLine (string.Format ("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
				}
			}
		}


		string HttpPost (string accessUri)
		{
			//Prepare auth request 
			var webRequest = WebRequest.Create (accessUri);
			webRequest.ContentType = "application/x-www-form-urlencoded";
			webRequest.Method = "POST";
			webRequest.ContentLength = 0;

			webRequest.Headers.Add ("Ocp-Apim-Subscription-Key", this.subscriptionId);

			using (WebResponse webResponse = webRequest.GetResponse ())
			using (Stream stream = webResponse.GetResponseStream ())
			{
				var reader = new StreamReader (stream, Encoding.UTF8);
				this.jwtAuthToken = reader.ReadToEnd ();
			}

			return this.jwtAuthToken;
		}
	}
}