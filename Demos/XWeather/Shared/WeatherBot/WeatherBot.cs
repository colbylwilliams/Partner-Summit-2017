using System;
using System.Linq;
using Plugin.TextToSpeech;

namespace XWeather.WeatherBot
{
	public class WeatherBot// : IDisposable
	{
		const string CortanaAppId = "c413b2ef-382c-45bd-8ff0-f76d60e2a821";
		const float SpeechConfidenceThreshold = .80f;

		readonly AudioRecorderService recorder;
		readonly LUISApi luis;
		readonly BingSpeechApi speechApi;

		public event EventHandler<WeatherBotStateEventArgs> StateChanged;
		public event EventHandler<WeatherBotRequestEventArgs> WeatherRequestUnderstood;

		int failureCount;
		const int failureThreshold = 3;


		public WeatherBot (string BingSpeechApiKey, string LuisSubscriptionKey)
		{
			recorder = new AudioRecorderService ();
			luis = new LUISApi (CortanaAppId, LuisSubscriptionKey);
			speechApi = new BingSpeechApi (BingSpeechApiKey);

			recorder.AudioInputReceived += Recorder_AudioRecorded;
		}


		//public void Dispose ()
		//{
		//	Cancel ();

		//	recorder = null;
		//	luis = null;
		//	speechApi = null;
		//}


		/// <summary>
		/// Listens for command.
		/// </summary>
		public void ListenForCommand ()
		{
			StateChanged?.Invoke (this, new WeatherBotStateEventArgs (WeatherBotState.Listening));

			recorder.AudioInputReceived -= Recorder_AudioRecorded;
			recorder.AudioInputReceived += Recorder_AudioRecorded;

			recorder.StartRecording ();
		}


		public void Cancel ()
		{
			if (recorder != null)
			{
				recorder.AudioInputReceived -= Recorder_AudioRecorded;
				recorder.StopRecording ();
			}
		}


		async void Recorder_AudioRecorded (object sender, string audioFilePath)
		{
			//see if we have a valid audio file - this will return null in the case only silence was recorded
			if (audioFilePath != null)
			{
				StateChanged?.Invoke (this, new WeatherBotStateEventArgs (WeatherBotState.Working, Constants.Messages.ParsingFeedbackMsg));

				SpeechResult speechToTextResult = null;

				try
				{
					speechToTextResult = await speechApi.SpeechToTextAsync (audioFilePath);
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine ("Error talking to Bing Speech API: {0}", ex.Message);
				}

				if (!string.IsNullOrEmpty (speechToTextResult?.Name) && speechToTextResult.Confidence > SpeechConfidenceThreshold)
				{
					StateChanged?.Invoke (this, new WeatherBotStateEventArgs (WeatherBotState.Working, speechToTextResult?.Name));

					try
					{
						var luisResult = await luis.GetEntityFromLUIS (speechToTextResult.Name);

						if (luisResult?.intents != null)
						{
							processIntent (luisResult);
							return;
						}
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine ("Error talking to LUIS: {0}", ex.Message);
					}
				}
			}

			//if we make it here we've failed :(
			StateChanged?.Invoke (this, new WeatherBotStateEventArgs (WeatherBotState.Failure, Constants.Messages.NotUnderstoodResponse));
		}


		Intent findBestIntent (Intent [] intents)
		{
			if (intents.Length == 0)
			{
				return null;
			}

			if (intents.Length > 1)
			{
				if (intents.Any (e => e.score > 0))
				{
					return intents.OrderByDescending (i => i.score).FirstOrDefault ();
				}
			}

			return intents [0];
		}


		void processIntent (LUISResult result)
		{
			var intent = findBestIntent (result.intents);

			switch (intent?.intent)
			{
				case Constants.LUIS.Intents.CheckWeather:

					processCheckWeatherIntent (result.entities);

					break;
				default:
					processErrorResponse (result);
					break;
			}
		}


		void processCheckWeatherIntent (Entity [] entities)
		{
			var locationEntity = entities.FindLocationEntity ();
			var timeEntity = entities.FindTimeEntity ();

			if (locationEntity != null)
			{
				switch (locationEntity.type)
				{
					case Constants.LUIS.Entities.Location.Absolute:

						processWeatherRequest (locationEntity, timeEntity);
						return;

					case Constants.LUIS.Entities.Location.Implicit:

						if (locationEntity.IsCurrentLocation ())
						{
							processWeatherRequest (locationEntity, timeEntity);
							return;
						}
						break;
				}
			}

			//processErrorResponse (NoLocationResponse);

			//correct check weather intent, but no location provided?  We assume they want the weather here
			//	e.g. "What's the weather here" can result in no entities
			processWeatherRequest (Constants.LUIS.Entities.Location.Current, timeEntity);
		}


		void processWeatherRequest (Entity locationEntity, Entity timeEntity)
		{
			string response = null;

			switch (timeEntity?.type)
			{
				case Constants.LUIS.Entities.DateTime.TimeRange:
				case null:

					if (timeEntity?.resolution == null || timeEntity.resolution.time == Constants.LUIS.Entities.DateTime.Current)
					{
						response = string.Format (Constants.Messages.CheckWeatherResponseTemplate, locationEntity.entity);

						processSuccessfulRequest (response, locationEntity);
					}
					else if (!string.IsNullOrEmpty (timeEntity.resolution.time))
					{
						DateTime dateTime;

						//time-based request (not date), but possibly in the future?
						if (DateTime.TryParse (timeEntity.resolution.time, out dateTime))
						{
							response = string.Format (Constants.Messages.CheckFutureWeatherForecastResponseTemplate, locationEntity.entity, timeEntity.entity);

							processSuccessfulRequest (response, locationEntity, dateTime);
						}
						else //couldn't figure out the date
						{
							response = string.Format (Constants.Messages.CheckWeatherResponseTemplate, locationEntity.entity);

							processSuccessfulRequest (response, locationEntity);
						}
					}
					else
					{
						//correct check weather intent, but no valid/understood date/time entity provided?
						processErrorResponse (Constants.Messages.NotUnderstoodResponse);
					}

					break;

				case Constants.LUIS.Entities.DateTime.DateRange:

					if (timeEntity.resolution?.date != null)
					{
						//specific date request (in 2 days, tomorrow, etc.)
						DateTime date;

						//if the entity itself is a DateTime, we'll need to change the messaging a bit to use that date (e.g. they asked "on January 25, 2016")
						if (DateTime.TryParse (timeEntity.entity, out date))
						{
							var dateResponse = date.ToLongDateString ();
							response = string.Format (Constants.Messages.CheckFutureWeatherForecastResponseTemplate, locationEntity.entity, dateResponse);

							processSuccessfulRequest (response, locationEntity, date);
						}
						else //entity is not a date... this should work for queries like "tomorrow," "in 3 days," etc.
						{
							date = timeEntity.resolution.date.Value;
							response = string.Format (Constants.Messages.CheckFutureWeatherForecastResponseTemplate, locationEntity.entity, timeEntity.entity);

							processSuccessfulRequest (response, locationEntity, date);
						}
					}
					else if (timeEntity.resolution?.resolution_type == Constants.LUIS.Entities.DateTime.DurationResolution)
					{
						//TODO: figure out how to handle these on the weather side of things

						//date range forecast with no specific date, e.g. "5 day".. this comes thru as a "duration" resolution
						response = string.Format (Constants.Messages.CheckWeatherForecastResponseTemplate, locationEntity.entity);

						processSuccessfulRequest (response);
					}
					else
					{
						//correct check weather intent, but no valid/understood date/time entity provided?
						processErrorResponse (Constants.Messages.NotUnderstoodResponse);
					}

					break;
			}
		}


		void processSuccessfulRequest (string responseMsg, Entity locationEntity = null, DateTime? date = null)
		{
			failureCount = 0; //reset

			Speak (responseMsg);

			StateChanged?.Invoke (this, new WeatherBotStateEventArgs (WeatherBotState.Working, responseMsg));

			WeatherRequestUnderstood?.Invoke (this, new WeatherBotRequestEventArgs (locationEntity, date));
		}


		void processErrorResponse (LUISResult result)
		{
			failureCount++;
			var entity = result.entities.FindBestEntityByScore ();
			var response = failureCount < failureThreshold ? Constants.Messages.NotUnderstoodResponse : Constants.Messages.IrritatedResponseTemplate;

			if (failureCount < 3)
			{
				if (entity != null)
				{
					var entityWords = entity.resolution?.value != null ? entity.resolution.value : entity.entity;
					response = string.Format (Constants.Messages.NotUnderstoodResponseTemplate, entityWords);
				}
			}

			processErrorResponse (response);
		}


		void processErrorResponse (string responseMsg)
		{
			StateChanged?.Invoke (this, new WeatherBotStateEventArgs (WeatherBotState.Failure, responseMsg));

			Speak (responseMsg);
		}


		public void Speak (string msg, bool enqueue = false)
		{
			CrossTextToSpeech.Current.Speak (msg, enqueue);
		}
	}
}