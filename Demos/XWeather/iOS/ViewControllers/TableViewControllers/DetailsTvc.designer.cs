// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace XWeather.iOS
{
	[Register ("DetailsTvc")]
	partial class DetailsTvc
	{
		[Outlet]
		XWeather.iOS.DetailsTvHeader tableHeader { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (tableHeader != null) {
				tableHeader.Dispose ();
				tableHeader = null;
			}
		}
	}
}
