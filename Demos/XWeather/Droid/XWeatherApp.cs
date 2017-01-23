﻿using System;

using Android.App;
using Android.OS;
using Android.Runtime;
using Plugin.CurrentActivity;
using XWeather.Constants;

namespace XWeather.Droid
{
	/// <summary>
	/// Custom extension of the base Android.App.Application class, in order to add properties which
	/// are global to the application, but not persisted as settings.
	/// </summary>
	[Application (Icon = "@mipmap/icon", Label = "XWeather", Theme = "@style/WeatherTheme", AllowBackup = true
#if DEBUG
				  , Debuggable = true
#endif
				 )]
	[MetaData ("com.google.android.geo.API_KEY", Value = PrivateKeys.GoogleMapsApiKey)]
	public class XWeatherApp : Application, Application.IActivityLifecycleCallbacks
	{
		/// <summary>
		/// Base constructor which must be implemented if it is to successfully inherit from the Application class.
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="transfer"></param>
		public XWeatherApp (IntPtr handle, JniHandleOwnership transfer)
			: base (handle, transfer)
		{
		}


		public override void OnCreate ()
		{
			base.OnCreate ();

			RegisterActivityLifecycleCallbacks (this);

			//Bootstrap.Run (Context, this);
		}


		public override void OnTerminate ()
		{
			base.OnTerminate ();
			UnregisterActivityLifecycleCallbacks (this);
		}


		public void OnActivityCreated (Activity activity, Bundle savedInstanceState)
		{
			CrossCurrentActivity.Current.Activity = activity;
		}


		public void OnActivityDestroyed (Activity activity)
		{
		}


		public void OnActivityPaused (Activity activity)
		{
		}


		public void OnActivityResumed (Activity activity)
		{
			CrossCurrentActivity.Current.Activity = activity;
		}


		public void OnActivitySaveInstanceState (Activity activity, Bundle outState)
		{
		}


		public void OnActivityStarted (Activity activity)
		{
			CrossCurrentActivity.Current.Activity = activity;
		}


		public void OnActivityStopped (Activity activity)
		{
		}
	}
}