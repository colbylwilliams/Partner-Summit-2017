// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace XWeather.iOS
{
	[Register ("WeatherBotVc")]
	partial class WeatherBotVc
	{
		[Outlet]
		UIKit.UIButton addLocationButton { get; set; }

		[Outlet]
		UIKit.UIVisualEffectView backgroundView { get; set; }

		[Outlet]
		UIKit.UIView[] botStateViews { get; set; }

		[Outlet]
		UIKit.UIButton cancelButton { get; set; }

		[Outlet]
		UIKit.UIView containerView { get; set; }

		[Outlet]
		UIKit.UILabel failureReasonLabel { get; set; }

		[Outlet]
		UIKit.UIView failureView { get; set; }

		[Outlet]
		UIKit.UILabel listeningFeedbackLabel { get; set; }

		[Outlet]
		UIKit.UIView listeningView { get; set; }

		[Outlet]
		UIKit.UILabel successCalloutLabel { get; set; }

		[Outlet]
		UIKit.UIView successView { get; set; }

		[Outlet]
		UIKit.UIButton tryAgainButton { get; set; }

		[Outlet]
		UIKit.UILabel workingFeedbackLabel { get; set; }

		[Outlet]
		UIKit.UIView workingView { get; set; }

		[Action ("addLocationButtonClicked:")]
		partial void addLocationButtonClicked (Foundation.NSObject sender);

		[Action ("cancelClicked:")]
		partial void cancelClicked (Foundation.NSObject sender);

		[Action ("containerDoubleTapped:")]
		partial void containerDoubleTapped (Foundation.NSObject sender);

		[Action ("tryAgainButtonClicked:")]
		partial void tryAgainButtonClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (backgroundView != null) {
				backgroundView.Dispose ();
				backgroundView = null;
			}

			if (cancelButton != null) {
				cancelButton.Dispose ();
				cancelButton = null;
			}

			if (containerView != null) {
				containerView.Dispose ();
				containerView = null;
			}

			if (failureView != null) {
				failureView.Dispose ();
				failureView = null;
			}

			if (listeningView != null) {
				listeningView.Dispose ();
				listeningView = null;
			}

			if (successView != null) {
				successView.Dispose ();
				successView = null;
			}

			if (workingView != null) {
				workingView.Dispose ();
				workingView = null;
			}

			if (listeningFeedbackLabel != null) {
				listeningFeedbackLabel.Dispose ();
				listeningFeedbackLabel = null;
			}

			if (workingFeedbackLabel != null) {
				workingFeedbackLabel.Dispose ();
				workingFeedbackLabel = null;
			}

			if (failureReasonLabel != null) {
				failureReasonLabel.Dispose ();
				failureReasonLabel = null;
			}

			if (successCalloutLabel != null) {
				successCalloutLabel.Dispose ();
				successCalloutLabel = null;
			}

			if (tryAgainButton != null) {
				tryAgainButton.Dispose ();
				tryAgainButton = null;
			}

			if (addLocationButton != null) {
				addLocationButton.Dispose ();
				addLocationButton = null;
			}
		}
	}
}
