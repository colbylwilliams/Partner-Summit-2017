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
			floatingButtons = coordLayout.FindSubViewsOfType <FloatingActionButton> ();
			floatingButtons.Reverse ();
			floatingButtons.ForAll (v => v.SetOnClickListener (this));

			setupViewPager ();

			getData ();

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


		public void OnClick (View v)
		{
			switch ((FabActions)floatingButtons.IndexOf ((FloatingActionButton)v))
			{
				case FabActions.Main:
					if (fabExpanded)
					{
						floatingButtons.First ().StartAnimation (animations [Resource.Animation.fab_rotate_close]);

						floatingButtons.Skip (1).ForAll (fab =>
						 {
							 fab.StartAnimation (animations [Resource.Animation.fab_close]);
							 fab.Clickable = false;
							 fab.Visibility = ViewStates.Invisible;
						 });

						fabExpanded = false;
					}
					else
					{
						floatingButtons.First ().StartAnimation (animations [Resource.Animation.fab_rotate_open]);

						floatingButtons.Skip (1).ForAll (fab =>
						 {
							 fab.StartAnimation (animations [Resource.Animation.fab_open]);
							 fab.Visibility = ViewStates.Visible;
							 fab.Clickable = true;
						 });

						fabExpanded = true;
					}
					break;
				case FabActions.Settings:
					StartActivity (typeof (LocationsActivity));
					break;
				case FabActions.WeatherBot:
					//do weatherbot UI here
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

				floatingButtons.First().Show ();

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
			var location = WuClient.Shared.Selected;

			var random = location == null || Settings.RandomBackgrounds;

			var gradients = location.GetTimeOfDayGradient (random);

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
	}
}