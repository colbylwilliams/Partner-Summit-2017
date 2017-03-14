using System;
using System.Collections.Generic;
using Android.Views;

namespace XWeather.Droid
{
	public static class ViewExtensions
	{
		public static List<TView> FindSubViewsOfType<TView> (this View view)
		where TView : View
		{
			if (view is ViewGroup)
			{
				return ((ViewGroup)view).FindSubViewsOfType<TView> ();
			}

			return new List<TView> ();
		}


		public static List<TView> FindSubViewsOfType<TView> (this IViewParent view)
		where TView : View
		{
			if (view is ViewGroup)
			{
				return ((ViewGroup)view).FindSubViewsOfType<TView> ();
			}

			return new List<TView> ();
		}


		public static List<TView> FindSubViewsOfType<TView>(this ViewGroup viewGroup)
		where TView : View
		{
			List<TView> views = new List<TView> ();

			for (int i = 0; i < viewGroup.ChildCount; i++)
			{
				View view = viewGroup.GetChildAt (i);

				if (view is TView)
				{
					views.Add ((TView)view);
				}
			}

			return views;
		}


		public static void ForAll (this IEnumerable<View> views, Action<View> action)
		{
			foreach (var view in views)
			{
				action (view);
			}
		}
	}
}