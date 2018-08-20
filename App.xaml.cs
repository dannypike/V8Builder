// Copyright 2018 Dan Pike
// Use of this source code is governed by a MIT license that can be
// found in the LICENSE file.

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace V8Builder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Thread GuiThread { get; private set; }
        public static Dispatcher GuiDispatcher { get; private set; }
        public string ProcessFolder { get; private set; }
        public string ConfigFolder { get; private set; }
        public string DownloadsFolder { get; private set; }
        public string Title { get; private set; }
        public string Version { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            GuiThread = Thread.CurrentThread;
            GuiDispatcher = Dispatcher;

            ProcessFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            ConfigFolder = Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%")
                , "Gamaliel", "V8Builder");
            if (!Directory.Exists(ConfigFolder))
            {
                Directory.CreateDirectory(ConfigFolder);
            }
            DownloadsFolder = Path.Combine(ConfigFolder, "Downloads");
            if (!Directory.Exists(DownloadsFolder))
            {
                Directory.CreateDirectory(DownloadsFolder);
            }

            // Load the V8Builder configuration
            var configFile = new FileInfo(Path.Combine(ConfigFolder, "v8builder.yaml"));
            if (!configFile.Exists)
            {
                File.Copy(Path.Combine(ProcessFolder, "v8builder.yaml"), configFile.FullName);
            }
            Configuration.Config.Load(configFile);

            AnnounceVersion();
        }

        public void AnnounceVersion()
        {
            // Get the assembly details
            Assembly assembly = Assembly.GetExecutingAssembly();
            var exeFilename = assembly.Location;
            Version = assembly.GetName().Version.ToString();
            string buildDate = "???";
            FileInfo fileInfo = new FileInfo(exeFilename);
            buildDate = fileInfo.LastWriteTime.ToString("dd MMM yyyy HH:mm:ss");

            AssemblyTitleAttribute title = assembly.GetCustomAttributes(
                typeof(AssemblyTitleAttribute), false)[0] as AssemblyTitleAttribute;
            Title = $"{title.Title} v{Version} - {ConfigFolder}";
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Configuration.Config.Save();
        }
    }
}