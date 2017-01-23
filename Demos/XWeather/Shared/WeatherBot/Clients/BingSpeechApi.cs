using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Diagnostics;

namespace XWeather.WeatherBot
{
	public class BingSpeechApi
	{
		readonly Authentication auth;

		public BingSpeechApi (string subscriptionKey)
		{
			auth = new Authentication (subscriptionKey);
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

			string headerValue;
			string requestUri = "https://speech.platform.bing.com/recognize";

			/* URI Params. Refer to the README file for more information. */
			requestUri += @"?scenarios=smd";                                  // websearch is the other main option.
			requestUri += @"&appid=D4D52672-91D7-4C74-8AD8-42B1D98141A5";     // You must use this ID.
			requestUri += @"&locale=en-US";                                   // We support several other languages.  Refer to README file.
			requestUri += @"&device.os=wp7";
			requestUri += @"&version=3.0";
			requestUri += @"&format=json";
			requestUri += @"&instanceid=565D69FF-E928-4B7E-87DA-9A750B96D9E3";
			requestUri += @"&requestid=" + Guid.NewGuid ().ToString ();

			string host = @"speech.platform.bing.com";
			string contentType = @"audio/wav; codec=""audio/pcm""; samplerate=16000";

			/*
             * Input your own audio file or use read from a microphone stream directly.
             */
			string responseString;
			FileStream fs = null;

			var token = auth.GetAccessToken ();

			/*
			 * Create a header with the access_token property of the returned token
			 */
			headerValue = "Bearer " + token;

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
						buffer = new Byte [checked((uint)Math.Min (1024, (int)fs.Length))];

						while ((bytesRead = fs.Read (buffer, 0, buffer.Length)) != 0)
						{
							requestStream.Write (buffer, 0, bytesRead);
						}

						// Flush
						requestStream.Flush ();
					}

					/*
					 * Get the response from the service.
					 */
					using (WebResponse response = request.GetResponse ())
					{
						Debug.WriteLine (((HttpWebResponse)response).StatusCode);

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
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine (ex.Message);
			}

			return null;
		}

#endif
	}
}