﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CoreFoundation;

using Foundation;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;

using NSPersonalCloud;

using Photos;

using Sentry;
using Sentry.Protocol;

using SQLite;

using UIKit;

using Unishare.Apps.Common;
using Unishare.Apps.Common.Models;
using Unishare.Apps.DarwinCore;
using Unishare.Apps.DarwinCore.Models;

namespace Unishare.Apps.DarwinMobile
{
    [Register("AppDelegate")]
    public class AppDelegate : UIResponder, IUIApplicationDelegate
    {
        public UIWindow Window { get; private set; }

        private CFNotificationObserverToken networkNotification;

        [Export("application:willFinishLaunchingWithOptions:")]
        public bool WillFinishLaunching(UIApplication application, NSDictionary launchOptions)
        {
            SQLitePCL.Batteries_V2.Init();

            AppCenter.Start("60ed8f1c-4c08-4598-beef-c169eb0c2e53", typeof(Analytics), typeof(Crashes));
            Globals.Loggers = new LoggerFactory().AddSentry(config => {
                config.Dsn = "https://d0a8d714e2984642a530aa7deaca3498@o209874.ingest.sentry.io/5174354";
                config.Environment = "iOS";
                config.Release = application.GetBundleVersion();
            });

            var databasePath = Path.Combine(PathHelpers.SharedLibrary, "Preferences.sqlite3");
            Globals.Database = new SQLiteConnection(databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex);
            Globals.Database.CreateTable<KeyValueModel>();
            Globals.Database.CreateTable<CloudModel>();
            Globals.Database.CreateTable<NodeModel>();
            Globals.Database.CreateTable<PLAsset>();

            Globals.Database.SaveSetting(UserSettings.PhotoBackupInterval, "1");

            if (Globals.Database.Find<KeyValueModel>(UserSettings.EnableSharing) is null)
            {
                Globals.Database.SaveSetting(UserSettings.EnableSharing, "1");
            }

            var sharingEnabled = false;
            if (Globals.Database.CheckSetting(UserSettings.EnableSharing, "1"))
            {
                sharingEnabled = true;
                UIApplication.SharedApplication.IdleTimerDisabled = true;
            }

            try
            {
                if (!Directory.Exists(PathHelpers.Cache)) Directory.CreateDirectory(PathHelpers.Cache);
                Directory.CreateDirectory(PathHelpers.SharedContainer);
            }
            catch
            {
                // Ignore.
            }

            Globals.FileSystem = new SandboxedFileSystem(sharingEnabled ? PathHelpers.Documents : null);

            if (PHPhotoLibrary.AuthorizationStatus == PHAuthorizationStatus.Authorized &&
                Globals.Database.CheckSetting(UserSettings.EnbalePhotoSharing, "1"))
            {
                Globals.FileSystem.ArePhotosShared = true;
            }

            if (PHPhotoLibrary.AuthorizationStatus == PHAuthorizationStatus.Authorized &&
                Globals.Database.CheckSetting(UserSettings.AutoBackupPhotos, "1"))
            {
                Globals.BackupWorker = new PhotoLibraryExporter();
            }

            Globals.Storage = new AppleDataStorage();
            Globals.CloudManager = new PCLocalService(Globals.Storage, Globals.Loggers, Globals.FileSystem);
            Task.Run(() => Globals.CloudManager.StartService());
            return true;
        }

        [Export("application:didFinishLaunchingWithOptions:")]
        public bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            Window = new UIWindow(UIScreen.MainScreen.Bounds);
            if (Globals.Database.Table<CloudModel>().Count() > 0)
            {
                Window.RootViewController = UIStoryboard.FromName("Main", NSBundle.MainBundle).InstantiateViewController("MainScreen");
            }
            else
            {
                Window.RootViewController = UIStoryboard.FromName("Main", NSBundle.MainBundle).InstantiateViewController("WelcomeScreen");
                application.SetMinimumBackgroundFetchInterval(UIApplication.BackgroundFetchIntervalNever);
            }
            Window.MakeKeyAndVisible();
            return true;
        }

        [Export("applicationWillEnterForeground:")]
        public void WillEnterForeground(UIApplication application)
        {
            try
            {
                Globals.CloudManager?.StartNetwork(false);
                networkNotification = CFNotificationCenter.Darwin.AddObserver(Notifications.NetworkChange, null, ObserveNetworkChange, CFNotificationSuspensionBehavior.Coalesce);
            }
            catch
            {
                // Ignored.
            }
        }

        [Export("applicationDidEnterBackground:")]
        public void DidEnterBackground(UIApplication application)
        {
            try
            {
                if (networkNotification != null)
                {
                    CFNotificationCenter.Darwin.RemoveObserver(networkNotification);
                    networkNotification = null;
                }
            }
            catch
            {
                // Ignored.
            }
        }

        [Export("applicationWillTerminate:")]
        public void WillTerminate(UIApplication application)
        {
            Globals.CloudManager?.Dispose();
            Globals.Database?.Dispose();
            Globals.Loggers?.Dispose();
        }

        #region Background App Refresh

        [Export("application:performFetchWithCompletionHandler:")]
        public void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
        {
            SentrySdk.AddBreadcrumb("Background App Refresh triggered.");

            var cloud = Globals.CloudManager.PersonalClouds?.FirstOrDefault();
            if (cloud == null)
            {
                SentrySdk.CaptureMessage("Backup triggered while no Personal Cloud configured.", SentryLevel.Error);
                completionHandler?.Invoke(UIBackgroundFetchResult.Failed);
                return;
            }

            var path = Globals.Database.LoadSetting(UserSettings.PhotoBackupPrefix);
            if (string.IsNullOrEmpty(path))
            {
                SentrySdk.CaptureMessage("Photo sync not configured.", SentryLevel.Error);
                completionHandler?.Invoke(UIBackgroundFetchResult.Failed);
                return;
            }

            var worker = Globals.BackupWorker;
            if (worker == null)
            {
                SentrySdk.CaptureMessage("Photo sync worker not initialized.", SentryLevel.Error);
                completionHandler?.Invoke(UIBackgroundFetchResult.Failed);
                return;
            }

            Globals.CloudManager.NetworkRefeshNodes();
            Task.Delay(TimeSpan.FromSeconds(15)).Wait();

            try
            {
                var items = worker.StartBackup(cloud.RootFS, path).Result;
                if (items > 0) completionHandler?.Invoke(UIBackgroundFetchResult.NewData);
                else completionHandler?.Invoke(UIBackgroundFetchResult.NoData);
            }
            catch
            {
                completionHandler?.Invoke(UIBackgroundFetchResult.Failed);
            }
        }

        #endregion

        private void ObserveNetworkChange(string name, NSDictionary userInfo)
        {
            if (name != Notifications.NetworkChange) return;

            try
            {
                Globals.CloudManager?.StartNetwork(false);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}

