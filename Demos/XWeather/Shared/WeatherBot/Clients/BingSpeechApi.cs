using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

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


		HttpWebRequest createWebRequest (string audioFilePath)
		{
			string headerValue;
			StringBuilder requestUriBuilder = new StringBuilder (Constants.Endpoints.BingSpeechApi);

			requestUriBuilder.Append (@"?scenarios=smd");                               // websearch is the other main option.
			requestUriBuilder.Append (@"&appid=D4D52672-91D7-4C74-8AD8-42B1D98141A5");  // You must use this ID.
			requestUriBuilder.Append (@"&locale=en-US");                                // We support several other languages.  Refer to README file.
			requestUriBuilder.Append (@"&device.os=wp7");
			requestUriBuilder.Append (@"&version=3.0");
			requestUriBuilder.Append (@"&format=json");
			requestUriBuilder.Append (@"&instanceid=565D69FF-E928-4B7E-87DA-9A750B96D9E3");
			requestUriBuilder.AppendFormat (@"&requestid={0}", Guid.NewGuid ());

			string host = @"speech.platform.bing.com";
			string contentType = @"audio/wav; codec=""audio/pcm""; samplerate=16000";

			/*
             * Input your own audio file or use read from a microphone stream directly.
             */

			FileStream fs = null;

			/*
			 * Create a header with the access_token property of the returned token
			 */
			headerValue = "Bearer " + auth.Token;
			var requestUri = requestUriBuilder.ToString ();

			Debug.WriteLine ("Request Uri: " + requestUri + Environment.NewLine);

			HttpWebRequest request = null;
			request = (HttpWebRequest)WebRequest.Create (requestUri);
			request.SendChunked = true;
			request.Accept = @"application/json;text/xml";
			request.Method = "POST";
			request.ProtocolVersion = HttpVersion.Version11;
			request.Host = host;
			request.ContentType = contentType;
			request.Headers ["Authorization"] = headerValue;

			try
			{
				using (fs = new FileStream (audioFilePath, FileMode.Open, FileAccess.Read))
				{
					/*
					 * Open a request stream and write 1024 byte chunks in the stream one at a time.
					 */
					byte [] buffer = null;
					int bytesRead = 0;

					using (Stream requestStream = request.GetRequestStream ())
					{
						/*
						 * Read 1024 raw bytes from the input audio file.
						 */
						buffer = new byte [checked((uint)Math.Min (1024, (int)fs.Length))];

						while ((bytesRead = fs.Read (buffer, 0, buffer.Length)) != 0)
						{
							requestStream.Write (buffer, 0, bytesRead);
						}

						// Flush
						requestStream.Flush ();
					}
				}

				return request;
			}
			catch (Exception ex)
			{
				Debug.WriteLine (ex.Message);
				throw;
			}
		}


		WebResponse sendRequest (Func<HttpWebRequest> requestFactory)
		{
			try
			{
				var request = requestFactory ();
				var response = request.GetResponse ();

				//if we get a valid response (non-null & no exception), then reset our retry count
				if (response != null)
				{
					retryCount = 0;
					Debug.WriteLine (((HttpWebResponse)response).StatusCode);

					return response;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error in sendRequest: {0}", ex.Message);

				//handle expired auth token
				if (ex.CheckWebResponseStatus (HttpStatusCode.Forbidden) && retryCount < 1)
				{
					auth.RenewAccessToken ();
					retryCount++;

					return sendRequest (requestFactory);
				}
			}

			return null;
		}


		public async Task<SpeechResult> SpeechToTextAsync (string audioFilePath)
		{
			return await Task.Run (() => SpeechToText (audioFilePath));
		}


#if TEST_DATA

		Random rnd = new Random ();
		static int testIndex = 0;

		public SpeechResult SpeechToText (string audioFilePath)
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


		public SpeechResult SpeechToText (string audioFilePath)
		{
			auth.Init ();

			try
			{
				string responseString;

				/*
				 * Get the response from the service.
				 */
				using (var response = sendRequest (() => createWebRequest (audioFilePath)))
				using (StreamReader sr = new StreamReader (response.GetResponseStream ()))
				{
					responseString = sr.ReadToEnd ();
				}

				//Console.WriteLine (responseString);

				var root = JsonConvert.DeserializeObject<RootObject> (responseString);
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