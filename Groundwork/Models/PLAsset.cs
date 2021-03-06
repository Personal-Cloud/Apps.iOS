﻿using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Photos;

using SQLite;

namespace NSPersonalCloud.DarwinCore.Models
{
    [Table(TableNames.PhotoAssets)]
    public class PLAsset
    {
        [Ignore, JsonIgnore]
        public PHAsset Asset { get; set; }

        [Ignore, JsonIgnore]
        public IReadOnlyList<PHAssetResource> Resources { get; set; }

        [Ignore, JsonIgnore]
        public bool HasChanged { get; set; }

        [Ignore, JsonIgnore]//Properties have been populated
        public bool IsAvailable { get; set; }

        [PrimaryKey]
        public string Id { get; set; }

        public string FileName { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PLAssetType Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PLAssetTags Tags { get; set; }

        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
        public DateTime BackupDate { get; set; }

        public long Size { get; set; }

        public long Version { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PLAsset asset &&
                   (Id == asset.Id ||
                   EqualityComparer<PHAsset>.Default.Equals(Asset, asset.Asset));
        }

        public override int GetHashCode() => HashCode.Combine(Id);

        public void PopulateProperties()
        {
            if (IsAvailable)//Refresh has been called
            {
                return;
            }
            Version = 1;

            // Couldn't find usage. Therefor comment out
            //             if (Asset == null && !string.IsNullOrEmpty(Id))
            //             {
            //                 Asset = PHAsset.FetchAssetsUsingLocalIdentifiers(new[] { Id }, null).OfType<PHAsset>().FirstOrDefault();
            //                 if (Asset == null)
            //                 {
            //                     IsAvailable = false;
            //                     return;
            //                 }
            //                 IsAvailable = true;
            // 
            //                 if (Type != (PLAssetType) Asset.MediaType) HasChanged = true;
            //                 Type = (PLAssetType) Asset.MediaType;
            // 
            //                 if (Tags != (PLAssetTags) Asset.MediaSubtypes) HasChanged = true;
            //                 Tags = (PLAssetTags) Asset.MediaSubtypes;
            // 
            //                 var creation = Asset.CreationDate.ToDateTime();
            //                 if (creation != CreationDate) HasChanged = true;
            //                 CreationDate = creation;
            // 
            //                 var modification = Asset.ModificationDate.ToDateTime();
            //                 if (modification != ModificationDate) HasChanged = true;
            //                 ModificationDate = modification;
            // 
            //                 RefreshResources();
            //                 Size = Resources?.Sum(x => x.UserInfoGetSize() ?? 0) ?? 0;
            // 
            //                 return;
            //             }


            Id = Asset.LocalIdentifier;
            Type = (PLAssetType) Asset.MediaType;
            Tags = (PLAssetTags) Asset.MediaSubtypes;
            CreationDate = Asset.CreationDate.ToDateTime();
            ModificationDate = Asset.ModificationDate.ToDateTime();

            if (Resources == null || FileName == null) RefreshResources();
            if (Size == 0) Size = Resources?.Sum(x => x.UserInfoGetSize() ?? 0) ?? 0;

            IsAvailable = true;
            return;
        }

        private void RefreshResources()
        {
            var resources = PHAssetResource.GetAssetResources(Asset);
            var list = new List<PHAssetResource>(resources.Length);
            foreach (var resource in resources)
            {
                if (resource.ResourceType == PHAssetResourceType.Photo ||
                    resource.ResourceType == PHAssetResourceType.Video ||
                    resource.ResourceType == PHAssetResourceType.Audio)
                {
                    var dtstr = CreationDate.ToLocalTime().ToString("yyyy-MM-dd HH_mm");
                    FileName = $"{dtstr} {resource.OriginalFilename}";
                }

                list.Add(resource);
            }

            Resources = list.AsReadOnly();
        }
    }
}
