using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

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


		public async Task Init ()
		{
			if (string.IsNullOrEmpty (Token))
			{
				Token = await HttpPost (Constants.Endpoints.AuthApi);
			}
		}


		public async Task RenewAccessToken ()
		{
			Token = null;
			Token = await HttpPost (Constants.Endpoints.AuthApi);

			Debug.WriteLine (string.Format ("Renewed token for user: {0} is: {1}",
											subscriptionId,
											Token));
		}


		async Task<string> HttpPost (string accessUri)
		{
			try
			{
				using (var client = new HttpClient ())
				{
					var request = new HttpRequestMessage (HttpMethod.Post, accessUri);
					request.Content = new FormUrlEncodedContent (new KeyValuePair<string, string> [0]);

					client.DefaultRequestHeaders.Add ("Ocp-Apim-Subscription-Key", subscriptionId);

					var result = await client.SendAsync (request);
					string resultContent = await result.Content.ReadAsStringAsync ();

					return resultContent;
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