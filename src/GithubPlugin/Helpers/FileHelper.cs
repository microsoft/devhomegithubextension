﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace DevHome.Common.Services;

public static class FileHelper
{
#pragma warning disable CS8603 // Possible null reference return.
    public static T Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json);
        }

        return default;
    }
#pragma warning restore CS8603 // Possible null reference return.

    public static void Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileContent = JsonConvert.SerializeObject(content);
        File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
    }

    public static void Delete(string folderPath, string fileName)
    {
        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }
}
