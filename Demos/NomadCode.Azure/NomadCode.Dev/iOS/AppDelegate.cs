using Foundation;
using UIKit;
using NomadCode.Azure;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace NomadCode.Dev.iOS
{
	[Register ("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		public override UIWindow Window { get; set; }


		public override bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
		{
#if OFFLINE_SYNC_ENABLED

			AzureClient.Shared.RegisterTable<User> ();
			Task.Run (async () => await AzureClient.Shared.InitializeAzync ("https://{your-app}.azurewebsites.net"));
#else
			AzureClient.Shared.Initialize ("https://{your-app}.azurewebsites.net");
#endif
			return true;
		}

		public async Task TestSomeData ()
		{
			var user = new User ();


			AzureClient client = AzureClient.Shared;

#if OFFLINE_SYNC_ENABLED
			await client.SyncAsync<User> ();                            // Pushes local and pulls remote changes
#endif

			await client.GetAsync<User> ("12345");                      // returns User.Id == "12345

			await client.GetAsync<User> ();                             // returns the all user objects

			await client.GetAsync<User> (u => u.Age < 34);              // returns users where age < 34

			await client.FirstOrDefault<User> (u => u.Name == "Colby"); // returns first user with name "Colby"


			await client.SaveAsync (user);                              // inserts or updates new user

			await client.SaveAsync (new List<User> { user });           // inserts or updates each user in a list


			await client.DeleteAsync<User> ("12345");                   // deletes User with User.Id == "12345

			await client.DeleteAsync (user);                            // deletes the user

			await client.DeleteAsync (new List<User> { user });         // deletes each user in a list

			await client.DeleteAsync<User> (u => u.Age < 34);           // delets all users where age < 34


		}

		public override void OnResignActivation (UIApplication application)
		{
			// Invoked when the application is about to move from active to inactive state.
			// This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) 
			// or when the user quits the application and it begins the transition to the background state.
			// Games should use this method to pause the game.
		}

		public override void DidEnterBackground (UIApplication application)
		{
			// Use this method to release shared resources, save user data, invalidate timers and store the application state.
			// If your application supports background exection this method is called instead of WillTerminate when the user quits.
		}

		public override void WillEnterForeground (UIApplication application)
		{
			// Called as part of the transiton from background to active state.
			// Here you can undo many of the changes made on entering the background.
		}

		public override void OnActivated (UIApplication application)
		{
			// Restart any tasks that were paused (or not yet started) while the application was inactive. 
			// If the application was previously in the background, optionally refresh the user interface.
		}

		public override void WillTerminate (UIApplication application)
		{
			// Called when the application is about to terminate. Save data, if needed. See also DidEnterBackground.
		}
	}
}

