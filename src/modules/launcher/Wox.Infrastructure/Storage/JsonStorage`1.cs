﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using Wox.Plugin.Logger;

namespace Wox.Infrastructure.Storage
{
    /// <summary>
    /// Serialize object using json format.
    /// </summary>
    public class JsonStorage<T>
    {
        private static readonly IFileSystem FileSystem = new FileSystem();
        private static readonly IPath Path = FileSystem.Path;
        private static readonly IFile File = FileSystem.File;

        private readonly object _saveLock = new object();

        // use property initialization instead of DefaultValueAttribute
        // easier and flexible for default value of object
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        private static readonly JsonSerializerOptions _informationSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        };

        private T _data;

        // need a new directory name
        public const string DirectoryName = "Settings";
        public const string FileSuffix = ".json";

        public string FilePath { get; set; }

        public string DirectoryPath { get; set; }

        // This storage helper returns whether or not to delete the json storage items
        private const int _jsonStorage = 1;
        private StoragePowerToysVersionInfo _storageHelper;

        private string DefaultFileContent { get; set; }

        public virtual T Load()
        {
            _storageHelper = new StoragePowerToysVersionInfo(FilePath, _jsonStorage);

            if (File.Exists(FilePath))
            {
                var serialized = File.ReadAllText(FilePath);
                if (!string.IsNullOrWhiteSpace(serialized))
                {
                    Deserialize(serialized);
                }
                else
                {
                    LoadDefault();
                }
            }
            else
            {
                LoadDefault();
            }

            return _data.NonNull();
        }

        private void Deserialize(string serialized)
        {
            try
            {
                _data = JsonSerializer.Deserialize<T>(serialized, _serializerOptions);
            }
            catch (JsonException e)
            {
                LoadDefault();
                Log.Exception($"Deserialize error for json <{FilePath}>", e, GetType());
            }

            if (_data == null)
            {
                LoadDefault();
            }
        }

        private void LoadDefault()
        {
            if (File.Exists(FilePath))
            {
                BackupOriginFile();
            }

            _data = JsonSerializer.Deserialize<T>("{}", _serializerOptions);
            Save();
        }

        private void BackupOriginFile()
        {
            // Using InvariantCulture since this is internal
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fffffff", CultureInfo.InvariantCulture);
            var directory = Path.GetDirectoryName(FilePath).NonNull();
            var originName = Path.GetFileNameWithoutExtension(FilePath);
            var backupName = $"{originName}-{timestamp}{FileSuffix}";
            var backupPath = Path.Combine(directory, backupName);
            File.Copy(FilePath, backupPath, true);

            // todo give user notification for the backup process
        }

        public void Save()
        {
            lock (_saveLock)
            {
                try
                {
                    string serialized = JsonSerializer.Serialize(_data, _serializerOptions);
                    File.WriteAllText(FilePath, serialized);

                    Log.Info($"Saving cached data at <{FilePath}>", GetType());
                }
                catch (IOException e)
                {
                    Log.Exception($"Error in saving data at <{FilePath}>", e, GetType());
                }
            }
        }

        public void Clear()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
                LoadDefault();
                Log.Info($"Deleting cached data at <{FilePath}>", GetType());
            }
        }

        public void SaveInformationFile(T data)
        {
            lock (_saveLock)
            {
                DefaultFileContent = JsonSerializer.Serialize(data, _informationSerializerOptions);
                _storageHelper.Close(DefaultFileContent);
            }
        }

        public bool CheckVersionMismatch()
        {
            // Skip the fields check if the version hasn't changed.
            // This optimization prevents unnecessary fields processing when the cache
            // is already up to date, enhancing performance and reducing IO operations
            if (!_storageHelper.ClearCache)
            {
                return false;
            }

            return true;
        }

        public bool CheckWithInformationFileToClear(T actualData)
        {
            var infoFilePath = _storageHelper.GetFilePath(FilePath, _jsonStorage);

            if (actualData == null)
            {
                return false;
            }

            if (!File.Exists(infoFilePath))
            {
                DefaultFileContent = JsonSerializer.Serialize(actualData, _informationSerializerOptions);
                _storageHelper.Close(DefaultFileContent);
                return true;
            }

            try
            {
                var infoFileContent = File.ReadAllText(infoFilePath);
                var infoFields = JsonSerializer.Deserialize<Dictionary<string, object>>(infoFileContent, _informationSerializerOptions);

                if (infoFields != null && infoFields.TryGetValue("DefaultContent", out var defaultContent))
                {
                    // Deserialize the 'DefaultContent' into the actual data type for comparison
                    // T defaultContentData = JsonSerializer.Deserialize<T>(defaultContent.ToString(), _informationSerializerOptions);

                    // Compare structures
                    try
                    {
                        JsonSerializer.Deserialize<T>(File.ReadAllText(FilePath), _informationSerializerOptions);

                        // Structures are compatible, retain the defaultContent
                        DefaultFileContent = defaultContent?.ToString();
                    }
                    catch (JsonException)
                    {
                        // Structures are not compatible, update with actualData
                        DefaultFileContent = JsonSerializer.Serialize(actualData, _informationSerializerOptions);
                        return false;
                    }
                }
                else
                {
                    // DefaultContent not found or infoFields is null, update with actualData
                    DefaultFileContent = JsonSerializer.Serialize(actualData, _informationSerializerOptions);
                }

                _storageHelper.Close(DefaultFileContent);
                return true;
            }
            catch (JsonException e)
            {
                Log.Exception($"Error in CheckWithInformationFileToClear at <{FilePath}>", e, GetType());
                return false;
            }
        }
    }
}
