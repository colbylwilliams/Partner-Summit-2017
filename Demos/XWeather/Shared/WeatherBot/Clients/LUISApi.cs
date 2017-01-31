using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace XWeather.WeatherBot
{
	public class LUISApi
	{
		readonly string appId;
		readonly string subscriptionId;

		public LUISApi (string appId, string subscriptionId)
		{
			this.appId = appId;
			this.subscriptionId = subscriptionId;
		}


#if TEST_DATA

		public async Task<LUISResult> GetResultForQuery (string query)
		{
			query = Uri.EscapeDataString (query);
			var personalFolder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			var jsonFilePath = Path.Combine (personalFolder, $"LUIS_{query}.json");

			if (File.Exists (jsonFilePath))
			{
				var json = File.ReadAllText (jsonFilePath);

				System.Threading.Thread.Sleep (500);

				return JsonConvert.DeserializeObject<LUISResult> (json);
			}

			return null;
		}

#else

		public async Task<LUISResult> GetResultForQuery (string query)
		{
			query = Uri.EscapeDataString (query);
			LUISResult result = null;

			using (HttpClient client = new HttpClient ())
			{
				string requestUri = $"{Constants.Endpoints.LUISApi}{appId}?subscription-key={subscriptionId}&q={query}";
				var response = await client.GetStringAsync (requestUri);

				//write test/offline data
				//var personalFolder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
				//var jsonFilePath = Path.Combine (personalFolder, $"LUIS_{query}.json");

				//if (!File.Exists (jsonFilePath))
				//	File.WriteAllText (jsonFilePath, response);


				result = JsonConvert.DeserializeObject<LUISResult> (response);
			}

			return result;
		}

#endif
	}
}