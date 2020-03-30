using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundation;

using NSPersonalCloud.RootFS;

using Photos;

using UIKit;

using Unishare.Apps.Common;
using Unishare.Apps.DarwinCore;
using Unishare.Apps.DarwinCore.Models;

namespace Unishare.Apps.DarwinMobile
{
    public partial class PhotosBackupController : UITableViewController
    {
        public PhotosBackupController(IntPtr handle) : base(handle) { }

        private const string ChooseDeviceSegue = "ChooseBackupPath";
        private const string ViewPhotosSegue = "ViewPhotos";

        private bool autoBackup;
        private string backupPath;
        private int backupIntervalHours;

        private IReadOnlyList<PLAsset> photos;
        private bool isBackupInProgress;
        private RootFileSystem fileSystem;

        #region Lifecycle

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            fileSystem = Globals.CloudManager.PersonalClouds[0].RootFS;

            /*
            if (isBackupInProgress)
            {
                Task.Run(async () => {
                    await Globals.BackupWorker.BackupTask.ConfigureAwait(false);
                    TableView.ReloadSections(new NSIndexSet(2), UITableViewRowAnimation.Automatic);
                });
            }
            */
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            photos = Globals.BackupWorker?.Photos;
            isBackupInProgress = Globals.BackupWorker?.BackupTask?.IsCompleted == false;
            autoBackup = PHPhotoLibrary.AuthorizationStatus == PHAuthorizationStatus.Authorized && Globals.Database.CheckSetting(UserSettings.AutoBackupPhotos, "1");
            backupPath = Globals.Database.LoadSetting(UserSettings.PhotoBackupPrefix);
            if (!int.TryParse(Globals.Database.LoadSetting(UserSettings.PhotoBackupInterval) ?? "-1", out backupIntervalHours)) backupIntervalHours = 0;
        }

        #endregion

        #region Data Source

        public override nint NumberOfSections(UITableView tableView) => 1;

        public override nint RowsInSection(UITableView tableView, nint section)
        {
            return (int) section switch
            {
                0 => 5,
                1 => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(section))
            };
        }

        public override string TitleForHeader(UITableView tableView, nint section)
        {
            return (int) section switch
            {
                0 => "自动备份",
                1 => "手动备份",
                _ => throw new ArgumentOutOfRangeException(nameof(section))
            };
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            if (indexPath.Section == 0 && indexPath.Row == 0)
            {
                var cell = (SwitchCell) tableView.DequeueReusableCell(SwitchCell.Identifier, indexPath);
                cell.Update("定时备份照片", autoBackup);
                cell.Accessory = UITableViewCellAccessory.None;
                cell.Clicked += ToggleAutoBackup;
                return cell;
            }

            if (indexPath.Section == 0 && indexPath.Row == 1)
            {
                var cell = (KeyValueCell) tableView.DequeueReusableCell(KeyValueCell.Identifier, indexPath);
                cell.Update("选择存储位置", string.IsNullOrEmpty(backupPath) ? null : "已设置", true);
                cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
                return cell;
            }

            if (indexPath.Section == 0 && indexPath.Row == 2)
            {
                var cell = (KeyValueCell) tableView.DequeueReusableCell(KeyValueCell.Identifier, indexPath);
                cell.Update("设置间隔时间", backupIntervalHours < 1 ? null : "1 小时", true);
                cell.Accessory = UITableViewCellAccessory.None;
                return cell;
            }

            if (indexPath.Section == 0 && indexPath.Row == 3)
            {
                var cell = (BasicCell) tableView.DequeueReusableCell(BasicCell.Identifier, indexPath);
                cell.Update("查看下次备份项目", true);
                cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
                return cell;
            }

            if (indexPath.Section == 0 && indexPath.Row == 4)
            {
                var cell = (BasicCell) tableView.DequeueReusableCell(BasicCell.Identifier, indexPath);
                cell.Update("立即执行计划备份", Colors.BlueButton, true);
                cell.Accessory = UITableViewCellAccessory.None;
                return cell;
            }

            throw new ArgumentOutOfRangeException(nameof(indexPath));
        }

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
                this.ShowAlert("无法设置间隔时间", "尚不支持调整备份间隔时间。");
                return;
            }

            if (indexPath.Section == 0 && indexPath.Row == 3)
            {
                PerformSegue(ViewPhotosSegue, this);
                return;
            }

            if (indexPath.Section == 0 && indexPath.Row == 4)
            {
                if ((Globals.BackupWorker?.BackupTask?.IsCompleted ?? true) != true)
                {
                    this.ShowAlert("无法启动新备份", "当前有计划备份正在执行。");
                    return;
                }

                Task.Run(() => {
                    if (Globals.BackupWorker == null) Globals.BackupWorker = new PhotoLibraryExporter();
                    Globals.BackupWorker.StartBackup(fileSystem, backupPath);
                });
                this.ShowAlert("备份已启动", "计划备份正在执行。您可以继续使用个人云。");
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(indexPath));
        }

        #endregion

        private void ToggleAutoBackup(object sender, ToggledEventArgs e)
        {
            if (e.On)
            {
                PHPhotoLibrary.RequestAuthorization(status => {
                    if (status == PHAuthorizationStatus.Authorized) TurnOnAutoBackup();
                    else
                    {
                        TurnOffAutoBackup();
                        InvokeOnMainThread(() => {
                            TableView.ReloadRows(new[] { NSIndexPath.FromRowSection(0, 2) }, UITableViewRowAnimation.Fade);
                            this.ShowAlert("无法设置定时备份", "您已拒绝个人云访问“照片”，请前往系统设置 App 更改隐私授权。");
                        });
                    }
                });
            }
            else TurnOffAutoBackup();
        }

        private void TurnOnAutoBackup()
        {
            if (string.IsNullOrEmpty(backupPath))
            {
                TurnOffAutoBackup();
                InvokeOnMainThread(() => {
                    this.ShowAlert("无法使用定时备份", "您尚未选择备份存储位置，请点击“选择存储位置”。");
                });
                return;
            }

            if (backupIntervalHours < 1)
            {
                TurnOffAutoBackup();
                InvokeOnMainThread(() => {
                    this.ShowAlert("无法使用定时备份", "您尚未设置备份间隔时间，请点击“设置间隔时间”。");
                });
                return;
            }

            Globals.Database.SaveSetting(UserSettings.AutoBackupPhotos, "1");
            InvokeOnMainThread(() => UIApplication.SharedApplication.SetMinimumBackgroundFetchInterval(backupIntervalHours * 3600));
        }

        private void TurnOffAutoBackup()
        {
            Globals.Database.SaveSetting(UserSettings.AutoBackupPhotos, "0");
            UIApplication.SharedApplication.SetMinimumBackgroundFetchInterval(UIApplication.BackgroundFetchIntervalNever);
        }
    }
}