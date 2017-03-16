using System;
using System.Threading.Tasks;

using Android.App;
using Android.Animation;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Views.Animations;

using Android.Support.Design.Widget;
using Android.Support.V4.View;

using SettingsStudio;

using XWeather.Clients;
using XWeather.Domain;
using XWeather.Shared;
using System.Collections.Generic;
using System.Linq;
using XWeather.Constants;
using XWeather.WeatherBot;
using Android.Content;

namespace XWeather.Droid
{
	[Activity (Label = "XWeather", MainLauncher = true,
			   Icon = "@mipmap/icon", LaunchMode = Android.Content.PM.LaunchMode.SingleTop,
			   ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	public class WeatherActivity : BaseActivity, FloatingActionButton.IOnClickListener
	{
		bool analyticsStarted;
		bool fabExpanded;

		int viewPagerCache;

		ViewPager viewPager;

		List<FloatingActionButton> floatingButtons;
		Dictionary<int, Animation> animations;

		enum FabActions
		{
			Main,
			Settings,
			WeatherBot
		}

		WeatherPagerAdapter pagerAdapter;
		WeatherBot.WeatherBot weatherBot;
		AlertDialog currentDialog;
		WuLocation location;


		protected override void OnCreate (Bundle savedInstanceState)
		{
			Shared.Bootstrap.Run ();

			base.OnCreate (savedInstanceState);

			SetContentView (Resource.Layout.WeatherActivity);

			animations = this.LoadAnimations (Resource.Animation.fab_rotate_open,
											  Resource.Animation.fab_rotate_close,
											  Resource.Animation.fab_open,
											  Resource.Animation.fab_close);

			var coordLayout = FindViewById (Resource.Id.coordinatorLayout);
			floatingButtons = coordLayout.FindSubViewsOfType<FloatingActionButton> ();
			floatingButtons.Reverse ();
			floatingButtons.ForAll (v => v.SetOnClickListener (this));

			setupViewPager ();

			getData ();

			weatherBot = new WeatherBot.WeatherBot (PrivateKeys.CognitiveServices.BingSpeech, PrivateKeys.CognitiveServices.Luis);
			weatherBot.WeatherRequestUnderstood += WeatherBot_WeatherRequestUnderstood;
			weatherBot.StateChanged += WeatherBot_StateChanged;

			//AnalyticsManager.Shared.RegisterForHockeyAppUpdates (this);
		}


		protected override void OnStart ()
		{
			base.OnStart ();

			reloadData ();
		}


		protected override void OnResume ()
		{
			base.OnResume ();

			startPageTracking ();
		}


		protected override void OnPause ()
		{
			Analytics.TrackPageViewEnd (pagerAdapter.GetFragmentAtPosition (viewPager.CurrentItem), WuClient.Shared.Selected);

			base.OnPause ();
		}


		protected override void OnDestroy ()
		{
			weatherBot.WeatherRequestUnderstood -= WeatherBot_WeatherRequestUnderstood;
			weatherBot.StateChanged -= WeatherBot_StateChanged;

			base.OnDestroy ();
		}


		void fabExpand (bool expand)
		{
			if (fabExpanded != expand)
			{
				int openCloseAnimation = expand ? Resource.Animation.fab_rotate_open : Resource.Animation.fab_rotate_close;
				int buttonAnimation = expand ? Resource.Animation.fab_open : Resource.Animation.fab_close;

				floatingButtons.First ().StartAnimation (animations [openCloseAnimation]);

				floatingButtons.Skip (1).ForAll (fab =>
				 {
					 fab.StartAnimation (animations [buttonAnimation]);
					 fab.Visibility = expand ? ViewStates.Visible : ViewStates.Invisible;
					 fab.Clickable = expand;
				 });

				fabExpanded = expand;
			}
		}


		public void OnClick (View v)
		{
			//OnClick listener for Floating Action Buttons (FABs)
			switch ((FabActions)floatingButtons.IndexOf ((FloatingActionButton)v))
			{
				case FabActions.Main:

					fabExpand (!fabExpanded);

					break;
				case FabActions.Settings:

					fabExpand (false);
					StartActivity (typeof (LocationsActivity));

					break;
				case FabActions.WeatherBot:

					fabExpand (false);

					if (weatherBot.CognitiveServicesEnabled)
					{
						weatherBot.ListenForCommand ();
					}

					break;
			}
		}


		protected override void HandleUpdatedSelectedLocation (object sender, EventArgs e)
		{
			RunOnUiThread (() =>
			{
				reloadData ();

				if (!analyticsStarted)
				{
					analyticsStarted = true;

					startPageTracking ();
				}
			});
		}


		void reloadData ()
		{
			for (int i = 0; i < 3; i++)
			{
				var fragment = pagerAdapter.GetFragmentAtPosition (i) as IRecyclerViewFragment;

				fragment?.Adapter?.NotifyDataSetChanged ();
			}
		}


		void setupViewPager ()
		{
			pagerAdapter = new WeatherPagerAdapter (SupportFragmentManager);

			viewPager = (ViewPager)FindViewById (Resource.Id.viewPager);
			viewPager.Adapter = pagerAdapter;

			viewPager.CurrentItem = Settings.WeatherPage;

			updateBackground ();

			viewPager.PageSelected += (sender, e) =>
			{
				Analytics.TrackPageViewEnd (pagerAdapter.GetFragmentAtPosition (viewPagerCache), WuClient.Shared.Selected);

				Analytics.TrackPageViewStart (pagerAdapter.GetFragmentAtPosition (viewPager.CurrentItem), childPageName (viewPager.CurrentItem), WuClient.Shared.Selected);

				Settings.WeatherPage = e.Position;

				floatingButtons.First ().Show ();

				updateBackground ();
			};


			viewPager.PageScrollStateChanged += (sender, e) =>
			{
				switch (e.State)
				{
					case ViewPager.ScrollStateDragging:

						viewPagerCache = viewPager.CurrentItem;

						break;
					case ViewPager.ScrollStateIdle:

						var fragment = pagerAdapter?.GetFragmentAtPosition (viewPagerCache) as IRecyclerViewFragment;

						fragment?.RecyclerView?.ScrollToPosition (0);

						break;
				}
			};
		}


		void updateBackground ()
		{
			var selectedLocation = WuClient.Shared.Selected;

			var random = selectedLocation == null || Settings.RandomBackgrounds;

			var gradients = selectedLocation.GetTimeOfDayGradient (random);

			using (var gd = new GradientDrawable (GradientDrawable.Orientation.TopBottom, gradients.Item1.ToArray ()))
			{
				gd.SetCornerRadius (0f);

				if (viewPager.Background == null)
				{
					viewPager.Background = gd;

					Window.SetStatusBarColor (gradients.Item1 [0]);
					Window.SetNavigationBarColor (gradients.Item1 [1]);
				}
				else
				{
					var backgrounds = new Drawable [2];

					backgrounds [0] = viewPager.Background;
					backgrounds [1] = gd;

					var crossfader = new TransitionDrawable (backgrounds);

					viewPager.Background = crossfader;

					crossfader.StartTransition (1000);

					var statusBarAnimator = ValueAnimator.OfArgb (Window.StatusBarColor, gradients.Item1 [0]);

					statusBarAnimator.SetDuration (1000);
					statusBarAnimator.SetInterpolator (new AccelerateDecelerateInterpolator ());

					statusBarAnimator.Update += (sender, e) =>
					{
						var val = e.Animation.AnimatedValue as Java.Lang.Integer;

						var color = new Color ((int)val);

						Window.SetStatusBarColor (color);
					};

					var naviationBarAnimator = ValueAnimator.OfArgb (Window.NavigationBarColor, gradients.Item1 [1]);

					naviationBarAnimator.SetDuration (1000);
					naviationBarAnimator.SetInterpolator (new AccelerateDecelerateInterpolator ());

					naviationBarAnimator.Update += (sender, e) =>
					{
						var val = e.Animation.AnimatedValue as Java.Lang.Integer;

						var color = new Color ((int)val);

						Window.SetNavigationBarColor (color);
					};

					statusBarAnimator.Start ();
					naviationBarAnimator.Start ();
				}
			}
		}


		void startPageTracking ()
		{
			var current = pagerAdapter.GetFragmentAtPosition (viewPager.CurrentItem);

			if (current != null)
			{
				Analytics.TrackPageViewStart (current, childPageName (viewPager.CurrentItem), WuClient.Shared.Selected);
			}
		}


		Pages childPageName (int index)
		{
			if (index == 0) return Pages.WeatherDaily;
			if (index == 1) return Pages.WeatherHourly;
			if (index == 2) return Pages.WeatherDetails;

			return Pages.Unknown;
		}


#if DEBUG

		void getData () => Task.Run (async () =>
		{
			await Bootstrap.InitializeDataStore ();
			await TestDataProvider.InitTestDataAsync (this);
		});

#else

		LocationProvider LocationProvider;

		void getData ()
		{
			if (LocationProvider == null) LocationProvider = new LocationProvider (this);

			Task.Run (async () =>
			{
				await Bootstrap.InitializeDataStore ();

				var location = await LocationProvider.GetCurrentLocationCoordnatesAsync ();

				await WuClient.Shared.GetLocations (location);
			});
		}
#endif


		async void WeatherBot_WeatherRequestUnderstood (object sender, WeatherBotRequestEventArgs e)
		{
			await getLocationForecast (e.Location.entity, e.RequestDateTime, e.UseCurrentLocation);
		}


		// "entity": "Fort Thomas, KY"
		// "entity": "Fort Thomas Kentucky"
		// "entity": "Paris, France"
		async Task getLocationForecast (string entity, DateTime? date = null, bool useCurrentLocation = false)
		{
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

			RunOnUiThread (() =>
			{
				if (string.IsNullOrEmpty (forecastString))
				{
					updateViewState (WeatherBotState.Failure, $"Unable to find weather forecast for {entity}");
					weatherBot.Speak ($"Unable to find weather forecast for {entity}", true);
				}
				else
				{
					System.Diagnostics.Debug.WriteLine ($"{forecastString}");

					updateViewState (WeatherBotState.Success, $"Would you like to add {location.Name} to your saved locations?");

					weatherBot.Speak (forecastString, true);
				}
			});
		}


		void WeatherBot_StateChanged (object sender, WeatherBotStateEventArgs e)
		{
			RunOnUiThread (() =>
			{
				updateViewState (e.State, e.Message);
			});
		}


		void updateViewState (WeatherBotState state, string feedbackMsg = null)
		{
			System.Diagnostics.Debug.WriteLine ($"{state}");

			switch (state)
			{
				case WeatherBotState.Listening:
				case WeatherBotState.Working:

					if (!(currentDialog is ProgressDialog))
					{
						currentDialog = new ProgressDialog (this);
						currentDialog.SetCancelable (true);
						currentDialog.SetButton ((int)DialogButtonType.Negative, "Cancel", (sender, e) => currentDialog.Dismiss ());
						currentDialog.DismissEvent += (sender, e) => weatherBot.Cancel ();
					}

					break;
				case WeatherBotState.Success:
				case WeatherBotState.Failure:

					currentDialog.Dismiss ();

					using (var ab = new AlertDialog.Builder (this))
					{
						ab.SetTitle ($"{state.ToString ()}...")
						  .SetMessage (feedbackMsg)
						  .SetCancelable (true)
						  .SetNegativeButton ("Cancel", (sender, e) => currentDialog.Dismiss ());

						currentDialog = ab.Create ();
					}

					break;
			}

			currentDialog.SetTitle ($"{state.ToString ()}...");
			currentDialog.SetMessage (feedbackMsg);
			setBotStateIcon (state);
			setNeutralButtonForState (state);

			if (!currentDialog.IsShowing)
			{
				currentDialog.Show ();
			}
		}


		void setBotStateIcon (WeatherBotState state)
		{
			switch (state)
			{
				case WeatherBotState.Success:
					currentDialog.SetIcon (Resource.Drawable.i_check);
					break;
				case WeatherBotState.Failure:
					currentDialog.SetIcon (Resource.Drawable.i_error);
					break;
			}
		}


		void setNeutralButtonForState (WeatherBotState state)
		{
			switch (state)
			{
				case WeatherBotState.Success:

					currentDialog.SetButton ((int)DialogButtonType.Neutral, "Add Location", (sender, e) =>
					{
						if (location != null)
						{
							WuClient.Shared.AddLocation (location);
						}

						currentDialog.Dismiss ();
					});

					break;
				case WeatherBotState.Failure:

					currentDialog.SetButton ((int)DialogButtonType.Neutral, "Try Again", (sender, e) =>
					{
						//restart at the "listening..." state
						currentDialog.Dismiss ();
						currentDialog = null;
						weatherBot.ListenForCommand ();
					});

					break;
			}
		}
	}
}