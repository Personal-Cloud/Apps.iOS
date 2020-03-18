using System;

using UIKit;

namespace Unishare.Apps.DarwinMobile
{
    public partial class HomeController : UITabBarController
    {
        public HomeController (IntPtr handle) : base (handle) { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            SelectedIndex = 1;
        }
    }
}