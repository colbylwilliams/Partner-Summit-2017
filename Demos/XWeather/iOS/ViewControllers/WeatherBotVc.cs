using System;
using System.Linq;
using System.Threading.Tasks;

using Foundation;
using UIKit;

using XWeather.Clients;

using SettingsStudio;

using XWeather.WeatherBot;
using XWeather.Constants;

namespace XWeather.iOS
{
	public partial class WeatherBotVc : UIViewController
	{
		WuLocation location;
		WeatherBot.WeatherBot weatherBot;
		WeatherBotState currentState;

		public WeatherBotVc (IntPtr handle) : base (handle) { }


		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			makePretty ();

			cancelButton.Alpha = 0;

			weatherBot = new WeatherBot.WeatherBot (PrivateKeys.CognitiveServices.BingSpeech, PrivateKeys.CognitiveServices.Luis);
		}


		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);

			System.Diagnostics.Debug.WriteLine ($"{PresentingViewController?.DefinesPresentationContext}");

			if (weatherBot.CognitiveServicesEnabled)
			{
				weatherBot.StateChanged += WeatherBot_StateChanged;
				weatherBot.WeatherRequestUnderstood += WeatherBot_WeatherRequestUnderstood;

				weatherBot.ListenForCommand ();
			}
			else
			{
				updateViewState (WeatherBotState.Failure);
				updateFeedback ("Please see readme file to enable cognitive services");
			}
		}


		public override void ViewWillDisappear (bool animated)
		{
			weatherBot.StateChanged -= WeatherBot_StateChanged;
			weatherBot.WeatherRequestUnderstood -= WeatherBot_WeatherRequestUnderstood;

			base.ViewWillDisappear (animated);
		}


		void WeatherBot_StateChanged (object sender, WeatherBotStateEventArgs e)
		{
			BeginInvokeOnMainThread (() =>
			{
				updateViewState (e.State);

				if (!string.IsNullOrEmpty (e.Message))
				{
					updateFeedback (e.Message);
				}
			});
		}


		async void WeatherBot_WeatherRequestUnderstood (object sender, WeatherBotRequestEventArgs e)
		{
			await getLocationForecast (e.Location.entity, e.RequestDateTime, e.UseCurrentLocation);
		}


		int index = 2;

		partial void containerDoubleTapped (NSObject sender)
		{
			if (index > 4) index = 1;

			var state = (WeatherBotState)index;

			updateViewState (state);

			index++;
		}


		partial void cancelClicked (NSObject sender) => finish ();


		partial void addLocationButtonClicked (NSObject sender)
		{
			//TODO: need to handle locations that are already added?

			if (location != null)
			{
				WuClient.Shared.AddLocation (location);

				finish ();
			}
		}


		partial void tryAgainButtonClicked (NSObject sender)
		{
			if (weatherBot.CognitiveServicesEnabled)
			{
				weatherBot.Cancel ();

				weatherBot.ListenForCommand ();
			}
		}


		void updateViewState (WeatherBotState state)
		{
			System.Diagnostics.Debug.WriteLine ($"{state}");

			if (currentState != state)
			{
				currentState = state;

				var subview = getViewForState (state);

				if (!subview?.IsDescendantOfView (containerView) ?? false)
				{
					var current = containerView.Subviews.FirstOrDefault (v => v.Tag > 0);

					subview.Alpha = 0;

					containerView.AddSubview (subview);

					subview.ConstrainToFitParent (containerView);


					if (current == null)
					{
						UIView.Animate (0.2, () =>
						{
							subview.Alpha = 1;

							if (cancelButton.Alpha < 1) cancelButton.Alpha = 1;

						}, () =>
						{
							subview.Alpha = 1;
						});
					}
					else
					{
						UIView.Animate (0.2, () =>
						{
							current.Alpha = 0;

						}, () =>
						{
							UIView.Animate (0.2, () =>
							{
								subview.Alpha = 1;

								if (cancelButton.Alpha < 1) cancelButton.Alpha = 1;

							}, () =>
							{
								current.RemoveFromSuperview ();
							});
						});
					}
				}
			}
		}


		void updateFeedback (string feedback)
		{
			switch (currentState)
			{
				case WeatherBotState.Listening:
					listeningFeedbackLabel.Text = feedback;
					break;
				case WeatherBotState.Working:
					workingFeedbackLabel.Text = feedback;
					break;
				case WeatherBotState.Failure:
					failureReasonLabel.Text = feedback;
					break;
				case WeatherBotState.Success:
					successCalloutLabel.Text = feedback;
					break;
			}
		}


		UIView getViewForState (WeatherBotState state) => getViewForState ((int)state);

		UIView getViewForState (nint state)
		{
			if (state < 1 || state > 4) return null;

			return botStateViews.FirstOrDefault (v => v.Tag == state);
		}


		void finish ()
		{
			weatherBot.Cancel ();

			DismissViewController (true, null);
		}


		// "entity": "Fort Thomas, KY"
		// "entity": "Fort Thomas Kentucky"
		// "entity": "Paris, France"
		async Task getLocationForecast (string entity, DateTime? date = null, bool useCurrentLocation = false)
		{
			UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;

			if (useCurrentLocation)
			{
				location = WuClient.Shared.Selected;
			}
			else
			{
				// this gets the location without adding it to the saved locations
				location = await WuClient.Shared.SearchLocation (entity);
			}

			var forecastString = location?.ForecastString (Settings.UomTemperature, date);

			BeginInvokeOnMainThread (() =>
			{
				UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;

				if (string.IsNullOrEmpty (forecastString))
				{
					updateViewState (WeatherBotState.Failure);
					weatherBot.Speak ($"Unable to find weather forecast for {entity}", true);
					updateFeedback ($"Unable to find weather forecast for {entity}");
				}
				else
				{
					System.Diagnostics.Debug.WriteLine ($"{forecastString}");

					updateViewState (WeatherBotState.Success);
					updateFeedback ($"Would you like to add {location.Name} to your saved locations?");

					weatherBot.Speak (forecastString, true);
				}
			});
		}


		void makePretty ()
		{
			foreach (var button in new UIButton [] { cancelButton, tryAgainButton, addLocationButton })
			{
				button.Layer.BorderWidth = 1;
				button.Layer.BorderColor = button.TitleColor (UIControlState.Normal).CGColor;
				button.Layer.CornerRadius = 3;
			}

			backgroundView.Layer.CornerRadius = 5;
			backgroundView.Layer.MasksToBounds = true;
		}
	}
}