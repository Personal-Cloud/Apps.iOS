using System;
using System.Globalization;
using System.Threading.Tasks;

using Foundation;

using NSPersonalCloud.RootFS;

using Photos;

using UIKit;

using NSPersonalCloud.Common;
using NSPersonalCloud.DarwinCore;

namespace NSPersonalCloud.DarwinMobile
{
    public partial class PhotosBackupController : UITableViewController
    {
        public PhotosBackupController(IntPtr handle) : base(handle) { }

        private const string ChooseDeviceSegue = "ChooseBackupPath";
        private const string ViewPhotosSegue = "ViewPhotos";

        private bool autoBackup;
        private string backupPath;
        private int backupIntervalHours;
        private RootFileSystem fileSystem;

        private object IsEnablingBackupBtnCache;

        #region Lifecycle

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            fileSystem = Globals.CloudManager.PersonalClouds[0].RootFS;
            NavigationItem.LeftBarButtonItem.Clicked += ShowHelp;
            IsEnablingBackupBtnCache = null;
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            autoBackup = PHPhotoLibrary.AuthorizationStatus == PHAuthorizationStatus.Authorized && Globals.Database.CheckSetting(UserSettings.AutoBackupPhotos, "1");
            backupPath = Globals.Database.LoadSetting(UserSettings.PhotoBackupPrefix);
            if (!int.TryParse(Globals.Database.LoadSetting(UserSettings.PhotoBackupInterval) ?? "-1", out backupIntervalHours)) backupIntervalHours = 0;

        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            TableView.ReloadRows(new[] { NSIndexPath.FromRowSection(0, 0), 
                NSIndexPath.FromRowSection(1, 0)}, UITableViewRowAnimation.Automatic);
        }

        public override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
        {
            base.PrepareForSegue(segue, sender);
            if (segue.Identifier == ChooseDeviceSegue)
            {
                var navigation = (UINavigationController) segue.DestinationViewController;
                var chooser = (ChooseDeviceController) navigation.TopViewController;
                chooser.FileSystem = fileSystem;
                chooser.NavigationTitle = this.Localize("Backup.ChooseBackupLocation");
                chooser.PathSelected += (o, e) => {
                    Globals.Database.SaveSetting(UserSettings.PhotoBackupPrefix, e.Path);
                    backupPath = e.Path;
                    InvokeOnMainThread(() => {

                        if (!string.IsNullOrWhiteSpace(backupPath))
                        {
                            TurnOnAutoBackup(IsEnablingBackupBtnCache);
                        }
                        if (IsEnablingBackupBtnCache is UISwitch button) button.On = true;
                        IsEnablingBackupBtnCache = null;

                        TableView.ReloadRows(new[] { NSIndexPath.FromRowSection(0, 0), NSIndexPath.FromRowSection(1, 0)}, UITableViewRowAnimation.Automatic);
                    });
                };
                return;
            }
        }

        #endregion

        #region TableView Data Source

        public override nint NumberOfSections(UITableView tableView) => 1;

        public override nint RowsInSection(UITableView tableView, nint section)
        {
            return (int) section switch
            {
                0 => 4,
                _ => throw new ArgumentOutOfRangeException(nameof(section))
            };
        }

        public override string TitleForHeader(UITableView tableView, nint section)
        {
            return (int) section switch
            {
                0 => this.Localize("Backup.AutoBackup"),
                _ => throw new ArgumentOutOfRangeException(nameof(section))
            };
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            if (indexPath.Section == 0 && indexPath.Row == 0)
            {
                var cell = (SwitchCell) tableView.DequeueReusableCell(SwitchCell.Identifier, indexPath);
                cell.Update(this.Localize("Backup.EnableAutoBackup"), autoBackup);
                cell.Accessory = UITableViewCellAccessory.None;
                cell.Clicked += ToggleAutoBackup;
                return cell;
            }

            if (indexPath.Section == 0 && indexPath.Row == 1)
            {
                var cell = (KeyValueCell) tableView.DequeueReusableCell(KeyValueCell.Identifier, indexPath);
                cell.Update(this.Localize("Backup.ChooseLocation"), backupPath, true);
                cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
                return cell;
            }


            if (indexPath.Section == 0 && indexPath.Row == 2)
            {
                var cell = (BasicCell) tableView.DequeueReusableCell(BasicCell.Identifier, indexPath);
                cell.Update(this.Localize("Backup.ViewItems"), true);
                cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
                return cell;
            }

            if (indexPath.Section == 0 && indexPath.Row == 3)
            {
                var cell = (BasicCell) tableView.DequeueReusableCell(BasicCell.Identifier, indexPath);
                cell.Update(this.Localize("Backup.BackupNow"), Colors.BlueButton, true);
                cell.Accessory = UITableViewCellAccessory.None;
                return cell;
            }

            throw new ArgumentOutOfRangeException(nameof(indexPath));
        }

        #endregion

        #region TableView Delegate

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            tableView.DeselectRow(indexPath, true);

            if (indexPath.Section == 0 && indexPath.Row == 0) return;

            if (indexPath.Section == 0 && indexPath.Row == 1)
            {
                PerformSegue(ChooseDeviceSegue, this);
                return;
            }


            if (indexPath.Section == 0 && indexPath.Row == 2)
            {
                if (autoBackup) PerformSegue(ViewPhotosSegue, this);
                else this.ShowAlert(this.Localize("Backup.NotSetUp"), this.Localize("Backup.SetUpBeforeViewingItems"));
                return;
            }

            if (indexPath.Section == 0 && indexPath.Row == 3)
            {
                if ((Globals.BackupWorker?.BackupTask?.IsCompleted ?? true) != true)
                {
                    this.ShowAlert(this.Localize("Backup.CannotExecute"), this.Localize("Backup.AlreadyRunning"));
                    return;
                }

                if (!autoBackup)
                {
                    this.ShowAlert(this.Localize("Backup.NotSetUp"), this.Localize("Backup.SetUpBeforeExecuting"));
                    return;
                }

                Task.Run(async () => {
                    if (Globals.BackupWorker == null){ 
                        Globals.BackupWorker = new PhotoLibraryExporter();
                        await Globals.BackupWorker.Init().ConfigureAwait(false);
                    }
                    await Globals.BackupWorker.StartBackup(fileSystem, backupPath,false).ConfigureAwait(false);
                });
                this.ShowAlert(this.Localize("Backup.Executed"), this.Localize("Backup.NewBackupInProgress"));
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(indexPath));
        }

        #endregion

        private void ShowHelp(object sender, EventArgs e)
        {
            this.ShowAlert(this.Localize("Help.Backup"), this.Localize("Help.BackupPhotos"));
        }

        private void ToggleAutoBackup(object sender, ToggledEventArgs e)
        {
            if (e.On)
            {
                PHPhotoLibrary.RequestAuthorization(status => {
                    if (status == PHAuthorizationStatus.Authorized) InvokeOnMainThread(() => TurnOnAutoBackup(sender));
                    else InvokeOnMainThread(() => {
                        TurnOffAutoBackup(sender);
                        this.ShowAlert(this.Localize("Backup.CannotSetUp"), this.Localize("Permission.Photos"));
                    });
                });
            }
            else TurnOffAutoBackup(sender);
        }

        private void TurnOnAutoBackup(object obj)
        {
            if (string.IsNullOrEmpty(backupPath))
            {
                TurnOffAutoBackup(obj);
                //this.ShowAlert(this.Localize("Backup.CannotSetUp"), this.Localize("Backup.NoBackupLocation"));
                IsEnablingBackupBtnCache = obj;
                PerformSegue(ChooseDeviceSegue, this);
                return;
            }

            if (backupIntervalHours < 1)
            {
                TurnOffAutoBackup(obj);
                this.ShowAlert(this.Localize("Backup.CannotSetUp"), this.Localize("Backup.NoInterval"));
                return;
            }

            UIApplication.SharedApplication.SetMinimumBackgroundFetchInterval(UIApplication.BackgroundFetchIntervalMinimum);
            if (UIApplication.SharedApplication.BackgroundRefreshStatus == UIBackgroundRefreshStatus.Available)
            {
                Globals.Database.SaveSetting(UserSettings.AutoBackupPhotos, "1");
                autoBackup = true;
            }
            else
            {
                this.ShowAlert(this.Localize("Backup.BackgroundRefreshDisabled"), this.Localize("Permission.BackgroundRefresh"));
                TurnOffAutoBackup(obj);
            }
        }

        private void TurnOffAutoBackup(object obj)
        {
            if (obj is UISwitch button && button.On) button.On = false;
            UIApplication.SharedApplication.SetMinimumBackgroundFetchInterval(UIApplication.BackgroundFetchIntervalNever);
            Globals.Database.SaveSetting(UserSettings.AutoBackupPhotos, "0");
            autoBackup = false;
        }
    }
}
