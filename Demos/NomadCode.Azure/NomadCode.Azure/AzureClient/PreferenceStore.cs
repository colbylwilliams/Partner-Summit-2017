﻿#if __MOBILE__

using System;

#if __IOS__
using static Foundation.NSUserDefaults;
#else
using Android.App;
using Android.Preferences;
#endif

namespace NomadCode.Azure
{
	public partial class AzureClient // KeyStore
	{

		const string hyphen = "-";

		const string lastRefreshStorageKey = "LastRefresh";

		const string userIdStorageKey = "UserId";

		const string anonymousAccessUserIdStorageKey = "AnonymousAccessUserId";

#if __IOS__

		static void setSetting (string key, string value) => StandardUserDefaults.SetString (value, key);


		static void setSetting (string key, DateTime value) => setSetting (key, value.ToString ());


		static string stringForKey (string key) => StandardUserDefaults.StringForKey (key);


		static DateTime dateTimeForKey (string key)
		{
			DateTime outDateTime;

			return DateTime.TryParse (StandardUserDefaults.StringForKey (key), out outDateTime) ? outDateTime : default (DateTime);
		}

#else

		static void setSetting (string key, string value)
		{
			using (var sharedPreferences = PreferenceManager.GetDefaultSharedPreferences (Application.Context))
			using (var sharedPreferencesEditor = sharedPreferences.Edit ())
			{
				sharedPreferencesEditor.PutString (key, value);
				sharedPreferencesEditor.Commit ();
			}
		}


		static void setSetting (string key, DateTime value)
		{
			using (var sharedPreferences = PreferenceManager.GetDefaultSharedPreferences (Application.Context))
			using (var sharedPreferencesEditor = sharedPreferences.Edit ())
			{
				sharedPreferencesEditor.PutString (key, value.ToString ());
				sharedPreferencesEditor.Commit ();
			}
		}

		const string empty = "";
		static string stringForKey (string key, string defaultValue = empty)
		{
			using (var sharedPreferences = PreferenceManager.GetDefaultSharedPreferences (Application.Context))
				return sharedPreferences.GetString (key, defaultValue);

		}


		DateTime dateTimeForKey (string key)
		{
			DateTime outDateTime;

			return DateTime.TryParse (stringForKey (key), out outDateTime) ? outDateTime : default (DateTime);
		}

#endif


		DateTime lastRefresh {
			get { return dateTimeForKey (lastRefreshStorageKey); }
			set { setSetting (lastRefreshStorageKey, value); }
		}


		string userId {
			get { return stringForKey (userIdStorageKey); }
			set { setSetting (userIdStorageKey, value); }
		}


		static string anonymousAccessUserId {
			get {
				var anonymousId = stringForKey (anonymousAccessUserIdStorageKey);

				if (string.IsNullOrEmpty (anonymousId))
				{
					anonymousId = Guid.NewGuid ().ToString ("N");

					setSetting (anonymousAccessUserIdStorageKey, anonymousId);
				}

				return anonymousId;
			}
			//set { setSetting (anonymousAccessUserIdStorageKey, value); }
		}


		string syncQueryFormat => userId?.Replace (hyphen, string.Empty);


		bool shouldRefresh => DateTime.UtcNow.Ticks - lastRefresh.Ticks > TimeSpan.TicksPerHour;


		void resetPreferenceStore ()
		{
			lastRefresh = default (DateTime);
			userId = string.Empty;
		}
	}
}
#endif