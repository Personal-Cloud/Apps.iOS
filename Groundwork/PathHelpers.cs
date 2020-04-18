﻿using System.IO;

using Foundation;

namespace Unishare.Apps.DarwinCore
{
    public static class Paths
    {
        private static string appGroupContainer;
        private static string AppGroup
        {
            get {
                if (string.IsNullOrEmpty(appGroupContainer))
                {
                    appGroupContainer = NSFileManager.DefaultManager.GetContainerUrl("group.com.daoyehuo.Unishare").Path;
                }
                return appGroupContainer;
            }
        }

        public static string SharedDocuments => Path.Combine(AppGroup, "Documents");
        public static string SharedLibrary => Path.Combine(AppGroup, "Library");
        public static string SharedCaches => Path.Combine(AppGroup, "Library", "Caches");
        public static string SharedSupport => Path.Combine(AppGroup, "Library", "Application Support");

        private static string appDocuments;
        public static string Documents
        {
            get {
                if (string.IsNullOrEmpty(appDocuments))
                {
                    appDocuments = NSFileManager.DefaultManager.GetUrl(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User, null, true, out _).Path;
                }
                return appDocuments;
            }
        }

        private static string appLibrary;
        public static string Library
        {
            get {
                if (string.IsNullOrEmpty(appLibrary))
                {
                    appLibrary = NSFileManager.DefaultManager.GetUrl(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User, null, true, out _).Path;
                }
                return appLibrary;
            }
        }

        private static string appCaches;
        public static string Caches
        {
            get {
                if (string.IsNullOrEmpty(appCaches))
                {
                    var caches = NSFileManager.DefaultManager.GetUrl(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User, null, true, out _).Path;
                    appCaches = Path.Combine(caches, NSBundle.MainBundle.BundleIdentifier);
                }
                return appCaches;
            }
        }

        private static string appSupport;
        public static string Support
        {
            get {
                if (string.IsNullOrEmpty(appSupport))
                {
                    var support = NSFileManager.DefaultManager.GetUrl(NSSearchPathDirectory.ApplicationSupportDirectory, NSSearchPathDomain.User, null, true, out _).Path;
                    appSupport = Path.Combine(support, NSBundle.MainBundle.BundleIdentifier);
                }
                return appSupport;
            }
        }

        private static string appTemp;
        public static string Temporary
        {
            get {
                if (string.IsNullOrEmpty(appTemp))
                {
                    appTemp = NSFileManager.DefaultManager.GetTemporaryDirectory().Path;
                }
                return appTemp;
            }
        }

        public static string Favorites => Path.Combine(Documents, "Favorites");
        public static string PhotoRestore => Path.Combine(Temporary, "Restore");
        public static string WebApps => Path.Combine(Library, "Web Apps");

        public static void CreateCommonDirectories()
        {
            if (!Directory.Exists(Caches)) Directory.CreateDirectory(Caches);
            if (!Directory.Exists(Support)) Directory.CreateDirectory(Support);
            if (!Directory.Exists(Temporary)) Directory.CreateDirectory(Temporary);

            if (!Directory.Exists(Favorites)) Directory.CreateDirectory(Favorites);
            if (!Directory.Exists(PhotoRestore)) Directory.CreateDirectory(PhotoRestore);
            if (!Directory.Exists(WebApps)) Directory.CreateDirectory(WebApps);
        }

        public static void CreateSharedDirectories()
        {
            if (!Directory.Exists(SharedDocuments)) Directory.CreateDirectory(SharedDocuments);
            if (!Directory.Exists(SharedLibrary)) Directory.CreateDirectory(SharedLibrary);
            if (!Directory.Exists(SharedCaches)) Directory.CreateDirectory(SharedCaches);
            if (!Directory.Exists(SharedSupport)) Directory.CreateDirectory(SharedSupport);
        }
    }
}
