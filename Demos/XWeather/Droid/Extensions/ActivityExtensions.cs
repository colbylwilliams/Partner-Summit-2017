using System.Collections.Generic;
using Android.App;
using Android.Views.Animations;

namespace XWeather.Droid
{
	public static class ActivityExtensions
	{
		public static Dictionary<int, Animation> LoadAnimations (this Activity activity, params int [] ids)
		{
			Dictionary<int, Animation> animations = new Dictionary<int, Animation> ();

			foreach (var animId in ids)
			{
				animations.Add(animId,AnimationUtils.LoadAnimation (activity, animId));
			}

			return animations;
		}
	}
}