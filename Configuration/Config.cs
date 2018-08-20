// Copyright 2018 Dan Pike
// Use of this source code is governed by a MIT license that can be
// found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using YamlDotNet.Serialization;

namespace V8Builder.Configuration
{
    public class Config
    {
        [YamlIgnore]
        public FileInfo ConfigFile { get; set; }

        public static Config Root { get; private set; }

        public string DepotToolsUrl { get; set; } = "https://storage.googleapis.com/chrome-infra/depot_tools.zip";
        public string DepotToolsFolder { get; set; } = Path.Combine((Application.Current as App).ConfigFolder, "depot_tools");
        public string WindowsKitFolder { get; set; } = Environment.ExpandEnvironmentVariables("%WINDOWSSDKDIR%");
        public string SourceFolder { get; set; } = "C:\\v8";
        public IList<BuildOption> BuildOptions { get; set; } = new List<BuildOption>();

        public string BuildDescription { get; set; } = ""
            ;

        public string BuildFolder { get; set; } = string.Empty;
        public IList<string> BuildConfigurations { get; set; } = new List<string>();
        public string BuildConfiguration { get; set; } = string.Empty;

        public static void Load(FileInfo configFile)
        {
            var parser = new DeserializerBuilder()
                .WithNamingConvention(new YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention())
                .Build();
            using (var reader = configFile.OpenText())
            {
                Root = parser.Deserialize<Config>(reader) ?? new Config();
            }
            Root.ConfigFile = configFile;
        }

        public static void Save()
        {
            var renderer = new SerializerBuilder()
                .WithNamingConvention(new YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention())
                .Build()
                ;
            using (var writer = Root.ConfigFile.CreateText())
            {
                renderer.Serialize(writer, Root);
            }
        }
    }
}