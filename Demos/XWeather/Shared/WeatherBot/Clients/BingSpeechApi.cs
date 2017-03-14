using System;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using PCLStorage;
using System.Net.Http;
using System.Net.Http.Headers;

namespace XWeather.WeatherBot
{
	public class BingSpeechApi
	{
		int retryCount;

		readonly Authentication auth;

		public BingSpeechApi (string subscriptionKey)
		{
			auth = new Authentication (subscriptionKey);
		}


		async Task<HttpRequestMessage> createWebRequest (string audioFilePath)
		{
			StringBuilder requestUriBuilder = new StringBuilder (Constants.Endpoints.BingSpeechApi);

			requestUriBuilder.Append (@"?scenarios=smd");                               // websearch is the other main option.
			requestUriBuilder.Append (@"&appid=D4D52672-91D7-4C74-8AD8-42B1D98141A5");  // You must use this ID.
			requestUriBuilder.Append (@"&locale=en-US");                                // We support several other languages.  Refer to README file.
			requestUriBuilder.Append (@"&device.os=wp7");
			requestUriBuilder.Append (@"&version=3.0");
			requestUriBuilder.Append (@"&format=json");
			requestUriBuilder.Append (@"&instanceid=565D69FF-E928-4B7E-87DA-9A750B96D9E3");
			requestUriBuilder.AppendFormat (@"&requestid={0}", Guid.NewGuid ());

			var requestUri = requestUriBuilder.ToString ();

			Debug.WriteLine ("Request Uri: " + requestUri + Environment.NewLine);

			try
			{
				var request = new HttpRequestMessage (HttpMethod.Post, requestUri);

				request.Headers.TransferEncodingChunked = true;
				request.Headers.Authorization = new AuthenticationHeaderValue ("Bearer", auth.Token);
				request.Headers.Accept.ParseAdd ("application/json");
				request.Headers.Accept.ParseAdd ("text/xml");

				var root = FileSystem.Current.LocalStorage;
				var file = await root.GetFileAsync (audioFilePath);
				var stream = await file.OpenAsync (FileAccess.Read);
				// we'll dispose the StreamContent later after it's sent

				request.Content = new StreamContent (stream);
				request.Content.Headers.ContentType = new MediaTypeHeaderValue ("audio/wav");

				return request;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex.Message);
				throw;
			}
		}


		async Task<string> sendRequest (HttpRequestMessage request)
		{
			try
			{
				using (var client = new HttpClient ())
				{
					var response = await client.SendAsync (request);

					//if we get a valid response (non-null & no exception), then reset our retry count & return the response
					if (response != null)
					{
						retryCount = 0;
						Debug.WriteLine ($"sendRequest returned ${response.StatusCode}");

						return await response.Content.ReadAsStringAsync ();
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error in sendRequest: {0}", ex.Message);

				//handle expired auth token
				if (ex.CheckWebResponseStatus (HttpStatusCode.Forbidden) && retryCount < 1)
				{
					await auth.RenewAccessToken ();
					retryCount++;

					return await sendRequest (request);
				}
			}
			finally
			{
				//release the underlying file stream
				request.Content?.Dispose ();
			}

			return null;
		}


#if TEST_DATA

		Random rnd = new Random ();
		static int testIndex = 0;

		public async Task<SpeechResult> SpeechToText (string audioFilePath)
		{
			//var testIndex = rnd.Next (0, 9);

			var personalFolder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			var jsonFilePath = Path.Combine (personalFolder, $"Speech_{testIndex}.json");

			testIndex++;

			if (File.Exists (jsonFilePath))
			{
				var json = File.ReadAllText (jsonFilePath);

				System.Threading.Thread.Sleep (750);

				var root = JsonConvert.DeserializeObject<RootObject> (json);
				return root.results? [0];
			}

			return null;
		}

#else


		public async Task<SpeechResult> SpeechToText (string audioFilePath)
		{
			await auth.Init ();

			try
			{
				var request = await createWebRequest (audioFilePath);
				var response = await sendRequest (request);

				var root = JsonConvert.DeserializeObject<RootObject> (response);
				var result = root.results? [0];

				//write some test/offline data
				//for (var testResponseIndex = 0; testResponseIndex < 10; testResponseIndex++)
				//{
				//	var personalFolder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
				//	var jsonFilePath = Path.Combine (personalFolder, $"Speech_{testResponseIndex}.json");

				//	if (!File.Exists (jsonFilePath))
				//	{
				//		File.WriteAllText (jsonFilePath, responseString);
				//		break;
				//	}
				//}

				return result;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex.Message);
				throw;
			}
		}

#endif
	}
}