﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

using Newtonsoft.Json;

using NSPersonalCloud;
using NSPersonalCloud.Config;
using NSPersonalCloud.FileSharing.Aliyun;

using UIKit;

using Unishare.Apps.Common;
using Unishare.Apps.Common.Models;

namespace Unishare.Apps.DarwinMobile
{
    public class AppleDataStorage : IConfigStorage
    {
        public event EventHandler CloudSaved;

        public IEnumerable<PersonalCloudInfo> LoadCloud()
        {
            var deviceName = Globals.Database.LoadSetting(UserSettings.DeviceName) ?? UIDevice.CurrentDevice.Name;
            return Globals.Database.Table<CloudModel>().Select(x => {
                var alibaba = Globals.Database.Table<AlibabaOSS>().Where(y => y.Cloud == x.Id).Select(y => {
                    var config = new OssConfig {
                        OssEndpoint = y.Endpoint,
                        BucketName = y.Bucket,
                        AccessKeyId = y.AccessID,
                        AccessKeySecret = y.AccessSecret
                    };
                    return new StorageProviderInfo {
                        Type = StorageProviderInstance.TypeAliYun,
                        Name = y.Name,
                        Visibility = (StorageProviderVisibility) y.Visibility,
                        Settings = JsonConvert.SerializeObject(config)
                    };
                });
                var azure = Globals.Database.Table<AzureBlob>().Where(y => y.Cloud == x.Id).Select(y => {
                    var config = new AzureBlobConfig {
                        ConnectionString = y.Parameters,
                        BlobName = y.Container
                    };
                    return new StorageProviderInfo {
                        Type = StorageProviderInstance.TypeAzure,
                        Name = y.Name,
                        Visibility = (StorageProviderVisibility) y.Visibility,
                        Settings = JsonConvert.SerializeObject(config)
                    };
                });
                var providers = new List<StorageProviderInfo>();
                providers.AddRange(alibaba);
                providers.AddRange(azure);
                return new PersonalCloudInfo(providers) {
                    Id = x.Id.ToString("N"),
                    DisplayName = x.Name,
                    NodeDisplayName = deviceName,
                    MasterKey = Convert.FromBase64String(x.Key),
                    TimeStamp = x.Version
                };
            });
        }

        public ServiceConfiguration LoadConfiguration()
        {
            var id = Globals.Database.LoadSetting(UserSettings.DeviceId);
            if (id is null) return null;

            var port = int.Parse(Globals.Database.LoadSetting(UserSettings.DevicePort));
            if (port <= IPEndPoint.MinPort || port > IPEndPoint.MaxPort) throw new InvalidOperationException();
            return new ServiceConfiguration {
                Id = new Guid(id),
                Port = port
            };
        }

        public void SaveCloud(IEnumerable<PersonalCloudInfo> cloud)
        {
            Globals.Database.DeleteAll<CloudModel>();
            Globals.Database.DeleteAll<AlibabaOSS>();
            Globals.Database.DeleteAll<AzureBlob>();
            foreach (var item in cloud)
            {
                var id = new Guid(item.Id);
                Globals.Database.Insert(new CloudModel {
                    Id = id,
                    Name = item.DisplayName,
                    Key = Convert.ToBase64String(item.MasterKey),
                    Version = item.TimeStamp
                });

                foreach (var provider in item.StorageProviders)
                {
                    switch (provider.Type)
                    {
                        case StorageProviderInstance.TypeAliYun:
                        {
                            var config = JsonConvert.DeserializeObject<OssConfig>(provider.Settings);
                            var model = new AlibabaOSS {
                                Cloud = id,
                                Name = provider.Name,
                                Visibility = (int) provider.Visibility,
                                Endpoint = config.OssEndpoint,
                                Bucket = config.BucketName,
                                AccessID = config.AccessKeyId,
                                AccessSecret = config.AccessKeySecret
                            };
                            if (model.Id == Guid.Empty) model.Id = Guid.NewGuid();
                            Globals.Database.Insert(model);
                            continue;
                        }

                        case StorageProviderInstance.TypeAzure:
                        {
                            var config = JsonConvert.DeserializeObject<AzureBlobConfig>(provider.Settings);
                            var model = new AzureBlob {
                                Cloud = id,
                                Name = provider.Name,
                                Visibility = (int) provider.Visibility,
                                Parameters = config.ConnectionString,
                                Container = config.BlobName
                            };
                            if (model.Id == Guid.Empty) model.Id = Guid.NewGuid();
                            Globals.Database.Insert(model);
                            continue;
                        }
                    }
                }
            }

            CloudSaved?.Invoke(this, EventArgs.Empty);
        }

        public void SaveConfiguration(ServiceConfiguration config)
        {
            Globals.Database.SaveSetting(UserSettings.DeviceId, config.Id.ToString("N"));
            Globals.Database.SaveSetting(UserSettings.DevicePort, config.Port.ToString(CultureInfo.InvariantCulture));
        }

        #region Apps

        public void SaveApp(string appId, string cloudId, string config)
        {
            // Todo
        }

        public List<Tuple<string, string>> GetApp(string appId)
        {
            // Todo
            return new List<Tuple<string, string>>();
        }

        #endregion
    }
}
