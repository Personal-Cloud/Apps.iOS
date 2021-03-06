// This file has been autogenerated from a class added in the UI designer.

using System;
using System.IO;
using System.Linq;

using Foundation;
using UIKit;

namespace NSPersonalCloud.DarwinMobile.Base.lproj
{
	public partial class NewDevicePromptController : UIViewController
	{
		public NewDevicePromptController (IntPtr handle) : base (handle)
		{
		}
        public bool OpenWebViewUrl(UIWebView webView, NSUrlRequest request, UIWebViewNavigationType navigationType)
        {
            if (navigationType==UIWebViewNavigationType.LinkClicked)
            {
                UIApplication.SharedApplication.OpenUrl(request.Url);
                return false;

            }
            return true;
        }

        public override void ViewDidLoad()
        {
            web.ShouldStartLoad += OpenWebViewUrl;

            var descpath = "";
            var preferredLanguage = NSLocale.PreferredLanguages.First();
            if (preferredLanguage.StartsWith("zh", StringComparison.Ordinal))
            {
                descpath = Path.Combine(NSBundle.MainBundle.ResourcePath, "zh_CN.lproj", "NewDevicePromt.htm");
            }
            else
            {
                descpath = Path.Combine(NSBundle.MainBundle.ResourcePath, "Base.lproj", "NewDevicePromt.htm");
            }

            var fu = NSUrl.FromFilename(descpath);

            web.LoadRequest(NSUrlRequest.FromUrl(fu));
            base.ViewDidLoad();
        }

		partial void OnClickCancel(Foundation.NSObject sender)
        {
			this.DismissViewController(true, null);
        }

		partial void OpenWebSite(Foundation.NSObject sender)
        {
            var preferredLanguage = NSLocale.PreferredLanguages.First();
            if (preferredLanguage.StartsWith("zh", StringComparison.Ordinal))
            {
                UIApplication.SharedApplication.OpenUrl(NSUrl.FromString("https://personal.house/cn/"));
            }
            else
            {
                UIApplication.SharedApplication.OpenUrl(NSUrl.FromString("https://personal.house"));
            }
        }
	}
}
