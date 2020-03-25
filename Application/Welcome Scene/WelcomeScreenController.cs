using System;
using System.Linq;
using UIKit;
using Unishare.Apps.Common.Models;

namespace Unishare.Apps.DarwinMobile
{
    public partial class WelcomeScreenController : UIViewController
    {
        public WelcomeScreenController(IntPtr handle) : base(handle) { }

        private const string FinishingSegue = "FinishTutorial";

        private bool welcomeCompleted;

        #region Lifecycle

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            CreateCloudButton.Layer.CornerRadius = 10;
            CreateCloudButton.ClipsToBounds = true;
            AddCloudButton.Layer.CornerRadius = 10;
            AddCloudButton.ClipsToBounds = true;

            var mainWindow = UIApplication.SharedApplication.Windows[0];
            if (mainWindow.RootViewController != this) mainWindow.RootViewController = this;

            Globals.Storage.CloudSaved += (o, e) => welcomeCompleted = Globals.Database.Table<CloudModel>().Count() > 0;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            if (!welcomeCompleted) return;

            UIApplication.SharedApplication.SetMinimumBackgroundFetchInterval(TimeSpan.FromHours(1).TotalSeconds);
            PerformSegue(FinishingSegue, this);
            welcomeCompleted = false;
        }

        #endregion
    }
}
