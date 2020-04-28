using System;
using System.Threading.Tasks;
using Foundation;
using MobileCoreServices;
using Newtonsoft.Json;
using NSPersonalCloud;
using NSPersonalCloud.FileSharing.Aliyun;

using UIKit;

using Unishare.Apps.Common.Models;
using Unishare.Apps.DarwinCore;

namespace Unishare.Apps.DarwinMobile
{
    public partial class AliYunController : UITableViewController
    {
        public AliYunController(IntPtr handle) : base(handle) { }

        #region Lifecycle

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            SaveButton.Clicked += VerifyCredentials;
        }

        #endregion

        #region TableView Delegate

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            tableView.DeselectRow(indexPath, true);

            if (indexPath.Section == 1 && indexPath.Row == 0)
            {
                var text = UIPasteboard.General.GetValue(UTType.PlainText)?.ToString()?.Trim();
                if (text[0] == '{' && text[^1] == '}')
                {
                    try
                    {
                        var model = JsonConvert.DeserializeObject<OssConfig>(text);

                        Endpoint.Text = model.OssEndpoint;
                        BucketName.Text = model.BucketName;
                        AccessKeyID.Text = model.AccessKeyId;
                        AccessKeySecret.Text = model.AccessKeySecret;
                    }
                    catch
                    {
                        // Ignored.
                    }
                }
            }
        }

        #endregion

        private void VerifyCredentials(object sender, EventArgs e)
        {
            var name = ServiceName.Text;
            var invalidCharHit = false;
            foreach (var character in VirtualFileSystem.InvalidCharacters)
            {
                if (name?.Contains(character) == true) invalidCharHit = true;
            }
            if (string.IsNullOrEmpty(name) || invalidCharHit)
            {
                this.ShowAlert(this.Localize("Online.BadName"), this.Localize("Online.IllegalName"), action => {
                    ServiceName.BecomeFirstResponder();
                });
                return;
            }

            var endpoint = Endpoint.Text;
            if (string.IsNullOrEmpty(endpoint))
            {
                this.ShowAlert(this.Localize("Online.BadCredential"), this.Localize("AliYun.BadEndpoint"), action => {
                    Endpoint.BecomeFirstResponder();
                });
                return;
            }

            var bucket = BucketName.Text;
            if (string.IsNullOrEmpty(bucket))
            {
                this.ShowAlert(this.Localize("Online.BadCredential"), this.Localize("AliYun.BadBucket"), action => {
                    BucketName.BecomeFirstResponder();
                });
                return;
            }

            var accessId = AccessKeyID.Text;
            if (string.IsNullOrEmpty(accessId))
            {
                this.ShowAlert(this.Localize("Online.BadCredential"), this.Localize("AliYun.BadUserID"), action => {
                    AccessKeyID.BecomeFirstResponder();
                });
                return;
            }

            var accessSecret = AccessKeySecret.Text;
            if (string.IsNullOrEmpty(accessSecret))
            {
                this.ShowAlert(this.Localize("Online.BadCredential"), this.Localize("AliYun.BadUserSecret"), action => {
                    AccessKeySecret.BecomeFirstResponder();
                });
                return;
            }

            if (Globals.Database.Find<AlibabaOSS>(x => x.Name == name) != null)
            {
                this.ShowAlert(this.Localize("Online.ServiceAlreadyExists"), this.Localize("Online.ChooseADifferentName"), action => {
                    ServiceName.BecomeFirstResponder();
                });
                return;
            }

            var alert = UIAlertController.Create(this.Localize("Online.Verifying"), null, UIAlertControllerStyle.Alert);
            PresentViewController(alert, true, () => {
                Task.Run(() => {
                    var config = new OssConfig {
                        OssEndpoint = endpoint,
                        BucketName = bucket,
                        AccessKeyId = accessId,
                        AccessKeySecret = accessSecret
                    };

                    if (config.Verify())
                    {
                        try
                        {
                            Globals.CloudManager.AddStorageProvider(Globals.CloudManager.PersonalClouds[0].Id, name, config, StorageProviderVisibility.Private);
                            InvokeOnMainThread(() => {
                                DismissViewController(true, () => {
                                    NavigationController.DismissViewController(true, null);
                                });
                            });
                        }
                        catch
                        {
                            InvokeOnMainThread(() => {
                                DismissViewController(true, () => {
                                    this.ShowAlert(this.Localize("AliYun.CannotAddService"), this.Localize("Error.Internal"));
                                });
                            });
                        }
                    }
                    else
                    {
                        InvokeOnMainThread(() => {
                            DismissViewController(true, () => {
                                this.ShowAlert(this.Localize("Error.Authentication"), this.Localize("AliYun.Unauthorized"));
                            });
                        });
                    }
                });
            });
        }
    }
}