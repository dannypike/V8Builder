// Copyright 2018 Dan Pike
// Use of this source code is governed by a MIT license that can be
// found in the LICENSE file.

using Microsoft.VisualBasic.FileIO;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using V8Builder.Configuration;

namespace V8Builder
{
    public class MainViewModel : INotifyPropertyChanged
    {
        static MainViewModel()
        {
            var list = new List<char>();
            list.AddRange(Path.GetInvalidPathChars());
            list.Add(' ');
            invalidFileNameCharacters_ = list.ToArray();
        }

        public MainViewModel()
        {
            DownloadDepotTools = new CommandHandler { Action = OnDownloadDepotTools };
            UpdateDepotTools = new CommandHandler { Action = OnUpdateDepotTools };
            BrowseSource = new CommandHandler { Action = OnBrowseSource };
            DownloadSource = new CommandHandler { Action = OnDownloadSource };
            UpdateSource = new CommandHandler { Action = OnUpdateSource };
            GetOptionsAvailable = new CommandHandler { Action = OnUpdateOptions };
            OpenReadMe = new CommandHandler { Action = OnOpenReadMe };
            ExploreConfiguration = new CommandHandler { Action = OnExploreConfiguration };
            ConfigureBuild = new CommandHandler { Action = OnConfigureBuild };
            BrowseBuildFolder = new CommandHandler { Action = OnBrowseBuild };
            MoveAvailable = new CommandHandler { Action = OnMoveAvailable };
            MoveSelected = new CommandHandler { Action = OnMoveSelected };
            BrowseWindowsKit = new CommandHandler { Action = OnBrowseWindowsKit };
            BuildV8 = new CommandHandler { Action = OnBuildV8 };

            // The available options are those that are not selected
            foreach (var option in Config.BuildOptions)
            {
                if (option.Selected)
                {
                    SelectedOptions.Add(option);
                }
                else
                {
                    AvailableOptions.Add(option);
                }
            }
            InvokePropertyChanged(nameof(Config));
        }

        private void EnableButtons(bool enable)
        {
            UpdateDepotTools.Enabled = DownloadDepotTools.Enabled = UpdateDepotTools.Enabled =
                DownloadSource.Enabled = UpdateSource.Enabled = GetOptionsAvailable.Enabled =
                ConfigureBuild.Enabled = BuildV8.Enabled = enable;
        }

        private void OnBrowseWindowsKit()
        {
            var dlg = new CommonOpenFileDialog
            {
                DefaultDirectory = Config.WindowsKitFolder,
                EnsurePathExists = true,
                InitialDirectory = Config.WindowsKitFolder,
                IsFolderPicker = true,
                Multiselect = false,
                RestoreDirectory = false,
                Title = "Please select the folder that contains the Windows SDK Version 10"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Config.WindowsKitFolder = dlg.FileName;
                InvokePropertyChanged(nameof(Config));
            }
        }

        private void OnMoveSelected()
        {
            (Application.Current.MainWindow as MainWindow).MoveSelected();
        }

        private void OnMoveAvailable()
        {
            (Application.Current.MainWindow as MainWindow).MoveAvailable();
        }

        private void OnBrowseBuild()
        {
            var dlg = new CommonOpenFileDialog
            {
                DefaultDirectory = Config.BuildFolder,
                EnsurePathExists = true,
                InitialDirectory = Config.BuildFolder,
                IsFolderPicker = true,
                Multiselect = false,
                RestoreDirectory = false,
                Title = "Please select the folder that will receive the build configuration"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Config.WindowsKitFolder = dlg.FileName;
                InvokePropertyChanged(nameof(Config));
            }
        }

        private async void OnConfigureBuild()
        {
            try
            {
                EnableButtons(false);
                if (!CheckSourceFolder(false))
                {
                    return;
                }
                if (string.IsNullOrEmpty(Config.BuildConfiguration))
                {
                    MessageBox.Show("Please select a build configuration from the list box in the bottom-left", "No build configuration set");
                    return;
                }

                var buildFolder = string.IsNullOrEmpty(Config.BuildFolder) ? Config.BuildConfiguration : Config.BuildFolder;
                var buildFolderFullPath = Path.IsPathRooted(buildFolder) ? buildFolder : Path.Combine(Config.SourceFolder, "v8", "out.gn", buildFolder);
                if (Directory.Exists(buildFolderFullPath))
                {
                    try
                    {
                        FileSystem.DeleteDirectory(buildFolderFullPath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show($"Cannot delete the existing build folder '{buildFolderFullPath}'; is that folder open in another program?"
                            , $"Building V8 failed");
                        StatusText = $"failed to delete existing build folder '{buildFolderFullPath}'";
                        return;
                    }
                }

                var batch = new BatchFile
                {
                    WorkingDirectory = Path.Combine(Config.SourceFolder, "v8"),
                    Model = this,
                    Title = $"running v8gen.py to configure a build of {Config.BuildConfiguration} in \"{buildFolder}\"",
                };
                if (!CheckPython(batch.PythonFileName))
                {
                    return;
                }
                batch.Commands.Add(GetCommandLine());
                StatusText = batch.Title;
                StatusText = await batch.Run();
            }
            finally
            {
                EnableButtons(true);
            }
        }

        private bool CheckSourceFolder(bool mustExist)
        {
            try
            {
                Config.SourceFolder = Config.SourceFolder.Trim();
                if ((mustExist && !Directory.Exists(Config.SourceFolder))
                    || 0 <= Config.SourceFolder.IndexOfAny(invalidFileNameCharacters_.ToArray())
                    )
                {
                    MessageBox.Show($"The configured source folder '{Config.SourceFolder}' does not exist or contains invalid characters; "
                        + "please replace it with a path that exists and is valid (spaces are not allowed, because they can confuse the "
                        + "build scripts).");
                    StatusText = $"invalid source folder: '{Config.SourceFolder}'";
                    return false;
                }
                return true;
            }
            finally
            {
                InvokePropertyChanged(nameof(Config));
            }
        }

        private async void OnBuildV8()
        {
            try
            {
                EnableButtons(false);
                if (!CheckSourceFolder(false))
                {
                    return;
                }

                var buildFolder = string.IsNullOrEmpty(Config.BuildFolder) ? Config.BuildConfiguration : Config.BuildFolder;
                var batch = new BatchFile
                {
                    WorkingDirectory = Path.Combine(Config.SourceFolder, "v8"),
                    Model = this,
                    Title = $"running Ninja to build V8 with configuration {Config.BuildConfiguration} in \"{buildFolder}\"",
                };
                batch.Commands.Add($"ninja -C out.gn\\{buildFolder}");
                StatusText = batch.Title;
                StatusText = await batch.Run();

                //StatusText = $"running Ninja to build V8 with configuration {Config.BuildConfiguration} in \"{buildFolder}\"";
                //installer.WorkingDirectory = Path.Combine(Config.SourceFolder, "v8");
                //installer.Arguments = $"/k title Ninja is building V8 - if all is well, this is likely to take several minutes & ninja -C \"out.gn\\{buildFolder}\"";
                //await Task.Run(() => Process.Start(installer).WaitForExit());
                //StatusText = "Ready";
            }
            finally
            {
                EnableButtons(true);
            }
        }

        private void OnExploreConfiguration()
        {
            Process.Start("explorer", (Application.Current as App).ConfigFolder);
        }

        private void OnOpenReadMe()
        {
            Process.Start("https://github.com/dannypike/V8Builder/blob/master/README.md");
        }

        private void OnBrowseSource()
        {
            var dlg = new CommonOpenFileDialog
            {
                DefaultDirectory = Config.SourceFolder,
                EnsurePathExists = true,
                InitialDirectory = Config.SourceFolder,
                IsFolderPicker = true,
                Multiselect = false,
                RestoreDirectory = false,
                Title = "Please select the parent folder that will contain the v8 source folder"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Config.SourceFolder = dlg.FileName;
                InvokePropertyChanged(nameof(Config));
            }
        }

        private async void OnUpdateDepotTools()
        {
            try
            {
                EnableButtons(false);

                var batch = new BatchFile
                {
                    WorkingDirectory = Config.DepotToolsFolder,
                    Model = this,
                    Title = $"running CIPD and gclient to install the toolkits",
                };
                batch.Commands.Add("call cipd");
                batch.Commands.Add("call gclient");
                StatusText = batch.Title;
                StatusText = await batch.Run();
            }
            finally
            {
                EnableButtons(true);
            }
        }

        private async void OnDownloadDepotTools()
        {
            try
            {
                EnableButtons(false);

                if (Directory.Exists(Config.DepotToolsFolder))
                {
                    try
                    {
                        FileSystem.DeleteDirectory(Config.DepotToolsFolder, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Cannot delete the existing depot_tools folder; is that folder open in another program?"
                            , $"Downloading depot_tools failed");
                        StatusText = $"failed to delete existing folder '{Config.DepotToolsFolder}'";
                        return;
                    }
                }

                var zipFileName = Path.Combine((Application.Current as App).DownloadsFolder, "depot_tools.zip");
                StatusText = $"Downloading depot tools from {Config.DepotToolsUrl} to {zipFileName}";

                var webClient = new WebClient();
                webClient.DownloadProgressChanged += DepotToolsProgress;
                ActionProgressValue = 0;
                if (!await Task.Run(() => GetDepotTools(webClient, zipFileName)))
                {
                    StatusText = $"Failed to overwrite file '{zipFileName}'; is it open in another program?";
                    return;
                }
                webClient.DownloadProgressChanged -= DepotToolsProgress;
                ActionProgressValue = 90;

                var timer = new Timer(state =>
                {
                    var progress = ActionProgressValue;
                    if (progress < 100)
                    {
                        ActionProgressValue = progress + 0.1;
                    }
                }, null, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));

                StatusText = $"Unzipping depot tools file '{zipFileName}'";
                await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(zipFileName, Config.DepotToolsFolder));
                timer.Dispose();

                File.SetAttributes(Path.Combine(Config.DepotToolsFolder, ".git"), FileAttributes.Hidden);
                ActionProgressValue = 0;
                StatusText = "Ready";
            }
            finally
            {
                EnableButtons(true);
            }
        }

        private async Task<bool> GetDepotTools(WebClient webClient, string zipFileName)
        {
            if (File.Exists("depot_tools.zip"))
            {
                try
                {
                    FileSystem.DeleteFile("depot_tools.zip", UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                }
                catch (Exception) { }
            }
            if (File.Exists("depot_tools.zip"))
            {
                return false;
            }
            await webClient.DownloadFileTaskAsync(Config.DepotToolsUrl, zipFileName);
            return true;
        }

        private void DepotToolsProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            ActionProgressValue = 0.9 * e.ProgressPercentage;
        }

        private async void OnDownloadSource()
        {
            try
            {
                EnableButtons(false);
                if (!CheckSourceFolder(false))
                {
                    return;
                }

                if (Directory.Exists(Config.SourceFolder))
                {
                    try
                    {
                        FileSystem.DeleteDirectory(Config.SourceFolder, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show($"Cannot delete the existing V8 source folder '{Config.SourceFolder}'; is that folder open in another program?"
                            , $"Downloading V8 source failed");
                        StatusText = $"failed to delete existing source folder '{Config.SourceFolder}'";
                        return;
                    }
                }
                Directory.CreateDirectory(Config.SourceFolder);

                var batch = new BatchFile
                {
                    WorkingDirectory = Config.SourceFolder,
                    Model = this,
                    Title = "Fetching v8 source code",
                };
                if (!CheckPython(batch.PythonFileName))
                {
                    return;
                }
                batch.Commands.Add($"\"{batch.PythonFileName}\" \"{Path.Combine(Config.DepotToolsFolder, "fetch.py")}\" v8");
                StatusText = batch.Title;
                StatusText = await batch.Run();
                //var installer = CreateInstaller();

                //StatusText = "Fetching v8 source code";
                //installer.WorkingDirectory = Config.SourceFolder;
                //installer.Arguments = "/k title Fetching v8 source code & fetch v8";
                //await Task.Run(() => Process.Start(installer).WaitForExit());
                //StatusText = "Ready";
            }
            finally
            {
                EnableButtons(true);
            }
        }

        private async void OnUpdateSource()
        {
            try
            {
                EnableButtons(false);
                if (!CheckSourceFolder(true))
                {
                    return;
                }

                var v8Folder = Path.Combine(Config.SourceFolder, "v8");
                if (!Directory.Exists(v8Folder))
                {
                    MessageBox.Show($"There is no V8 source to update in folder '{Config.SourceFolder}'; please select a different folder or "
                        + "use the DOWNLOAD button to download a new copy", "Updating V8 source failed");
                    StatusText = "Failed to update V8 source";
                    return;
                }

                var configFolder = (Application.Current as App).ConfigFolder;
                var configurationsFile = new FileInfo(Path.Combine(configFolder, "Downloads", "configurations.txt"));
                if (configurationsFile.Exists)
                {
                    FileSystem.DeleteFile(configurationsFile.FullName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                }

                var batch = new BatchFile
                {
                    WorkingDirectory = Path.Combine(Config.SourceFolder, "v8"),
                    Model = this,
                    Title = $"updating the v8 build configurations and source code",
                };
                if (!CheckPython(batch.PythonFileName))
                {
                    return;
                }
                batch.Commands.Add($"\"{batch.PythonFileName}\" tools\\dev\\v8gen.py list >{configurationsFile.FullName}");
                batch.Commands.Add($"call gclient sync");
                StatusText = batch.Title;
                StatusText = await batch.Run();

                //var installer = CreateInstaller();

                //StatusText = "running v8gen to get a list of the available configurations";
                //installer.WorkingDirectory = $"{Config.SourceFolder}\\v8";
                //installer.Arguments = "/k title v8gen - downloading a list of configurations & "
                //    + $"python tools\\dev\\v8gen.py list >{configurationsFile.FullName}";
                //await Task.Run(() => Process.Start(installer).WaitForExit());

                var configurations = File.ReadAllLines(configurationsFile.FullName)
                    .Select(cc => cc.Trim())
                    .Where(cc => !string.IsNullOrEmpty(cc))
                    ;
                var existingBuild = Config.BuildConfiguration;
                Config.BuildConfiguration = null;
                Config.BuildConfigurations.Clear();
                foreach (var configuration in configurations)
                {
                    if (configuration == existingBuild)
                    {
                        Config.BuildConfiguration = configuration;
                    }
                    Config.BuildConfigurations.Add(configuration);
                }
                InvokePropertyChanged(nameof(Config));

                //StatusText = "Updating v8 source code - this may take a few minutes";
                //installer.WorkingDirectory = v8Folder;
                //installer.Arguments = "/k title Updating v8 source code & gclient sync";
                //await Task.Run(() => Process.Start(installer).WaitForExit());
                //StatusText = "Ready";
            }
            finally
            {
                EnableButtons(true);
            }
        }

        private bool CheckPython(string pythonFileName)
        {
            if (!string.IsNullOrEmpty(pythonFileName) && File.Exists(pythonFileName))
            {
                return true;
            }
            MessageBox.Show("Could not find the required depot_tools; please update them from the Tools tab (downloading them first, if necessary)");
            StatusText = "Missing or invalid depot_tools; please download/update them";
            return false;
        }

        private enum OptionParserState
        {
            None,
            Name,
            Current,
            From,
            Blank,
            Description
        }

        private async void OnUpdateOptions()
        {
            try
            {
                EnableButtons(false);

                var workingFolder = Path.Combine(Config.SourceFolder, "v8");
                var configFolder = (Application.Current as App).ConfigFolder;
                var genFolder = Path.Combine(configFolder, "Downloads", "out.gn", "defaults");
                var argsFileName = Path.Combine(configFolder, "Downloads", "gn_defaults.txt");

                if (!Directory.Exists(workingFolder))
                {
                    MessageBox.Show($"You must select an existing V8 source folder for the depot_tools to be able to produce a list "
                        + "of available compiler options", $"V8 folder not found - '{workingFolder}'");
                    StatusText = "Updating build options failed";
                    return;
                }
                if (Directory.Exists(genFolder))
                {
                    try
                    {
                        FileSystem.DeleteDirectory(genFolder, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show($"Cannot delete '{genFolder}'; is that folder open in another program?"
                            , $"Building V8 failed");
                        StatusText = $"failed to delete '{genFolder}'";
                        return;
                    }
                }

                var batch = new BatchFile
                {
                    WorkingDirectory = workingFolder,
                    Model = this,
                    Title = $"retrieving the list of available build options",
                };
                if (!CheckPython(batch.PythonFileName))
                {
                    return;
                }

                // batch.Commands.Add($"call python tools\\dev\\v8gen.py gen -b {Config.BuildConfiguration} \"{genFolder}\" --no-goma");
                var gnFileName = Path.Combine(Config.DepotToolsFolder, "gn.py");
                batch.Commands.Add($"\"{batch.PythonFileName}\" \"{gnFileName}\" gen \"{genFolder}\"");
                batch.Commands.Add($"\"{batch.PythonFileName}\" \"{gnFileName}\" args \"{genFolder}\" --list >{argsFileName}");
                StatusText = batch.Title;
                StatusText = await batch.Run();

                //var installer = CreateInstaller();
                //StatusText = "Extracting build options";
                //installer.WorkingDirectory = Path.Combine(Config.SourceFolder, "v8");

                //installer.Arguments = $"/k title Generating a list of build options & gn gen \"{genFolder}\" & gn args \"{genFolder}\" --list >{argsFileName}";
                //await Task.Run(() => Process.Start(installer).WaitForExit());

                var existingOptions = new Dictionary<string, BuildOption>(StringComparer.OrdinalIgnoreCase);
                foreach (var option in Config.BuildOptions)
                {
                    existingOptions[option.Name] = option;
                }
                AvailableOptions.Clear();
                SelectedOptions.Clear();

                StatusText = "Parsing build options";
                Config.BuildOptions.Clear();
                var lines = File.ReadAllLines(argsFileName);
                BuildOption currentOption = null;
                OptionParserState state = OptionParserState.None;
                StringBuilder descriptionBuilder = null;
                for (var ii = 0; ii < lines.Length; ++ii)
                {
                    var line = lines[ii];
                    switch (state)
                    {
                        case OptionParserState.None:
                        case OptionParserState.Name:
                            descriptionBuilder = new StringBuilder();
                            var name = line.Trim();
                            if (existingOptions.TryGetValue(name, out currentOption))
                            {
                                existingOptions.Remove(name);
                            }
                            else
                            {
                                currentOption = new Configuration.BuildOption
                                {
                                    Name = line
                                };
                            }
                            if (currentOption.Selected)
                            {
                                SelectedOptions.Add(currentOption);
                            }
                            else
                            {
                                AvailableOptions.Add(currentOption);
                            }
                            Config.BuildOptions.Add(currentOption);
                            state = OptionParserState.Current;
                            break;

                        case OptionParserState.Current:
                            currentOption.Default = line.Split('=')[1].Trim();
                            state = OptionParserState.From;
                            break;

                        case OptionParserState.From:
                            state = OptionParserState.Blank;
                            break;

                        case OptionParserState.Blank:
                            state = OptionParserState.Description;
                            break;

                        case OptionParserState.Description:
                            if (!string.IsNullOrEmpty(line) && line[0] != ' ')
                            {
                                --ii;   // Reparse this line, it's the next name
                                currentOption.Description = descriptionBuilder.ToString().Trim();
                                state = OptionParserState.Name;
                            }
                            else
                            {
                                descriptionBuilder.Append(' ');
                                descriptionBuilder.Append(line.Trim());
                            }
                            break;
                    }
                }
                if (currentOption != null && descriptionBuilder != null && (descriptionBuilder.Length > 0))
                {
                    currentOption.Description = descriptionBuilder.ToString();
                }
                StatusText = "Ready";
            }
            finally
            {
                EnableButtons(true);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(params string[] propertyNames)
        {
            if (App.GuiDispatcher != null
                && App.GuiThread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId
                )
            {
                App.GuiDispatcher.Invoke(() => InvokePropertyChanged(propertyNames));
            }
            else
            {
                foreach (var propertyName in propertyNames)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        public int CurrentTab
        {
            get => currentTab_;
            set
            {
                currentTab_ = value;
                InvokePropertyChanged(nameof(CurrentTab));
            }
        }

        public Configuration.Config Config => Configuration.Config.Root;

        public string StatusText
        {
            get => statusText_;
            set
            {
                statusText_ = value;
                InvokePropertyChanged(nameof(StatusText));
            }
        }

        public double ActionProgressValue
        {
            get => progressValue_;
            set
            {
                var newValue = value == 0 ? 0 : Math.Max(progressValue_, value);
                if (newValue != progressValue_)
                {
                    progressValue_ = newValue;
                    InvokePropertyChanged(nameof(ActionProgressValue));
                }
            }
        }

        public ObservableCollection<Configuration.BuildOption> AvailableOptions { get; set; } = new ObservableCollection<BuildOption>();
        public ObservableCollection<Configuration.BuildOption> SelectedOptions { get; set; } = new ObservableCollection<BuildOption>();

        public BuildOption DescribingOption
        {
            get => describingOption_;
            set
            {
                describingOption_ = value;
                InvokePropertyChanged(nameof(DescribingOption));
            }
        }

        public string CommandLine
        {
            get => GetCommandLine() ?? "Select a configuration from the list box on the left "
                + "(click the UPDATE OPTIONS button, if the list box is empty)";
        }

        public bool CommandLineReadOnly { get; } = true;

        private string GetCommandLine()
        {
            if (string.IsNullOrEmpty(Config.BuildConfiguration))
            {
                return null;
            }
            var buildFolder = string.IsNullOrEmpty(Config.BuildFolder) ? Config.BuildConfiguration : Config.BuildFolder;
            var selectedOptions = SelectedOptions.Where(option => option.NewValue != null).ToList();
            var batch = new BatchFile();
            var sb = new StringBuilder($"{batch.PythonFileName} tools\\dev\\v8gen.py gen -b {Config.BuildConfiguration} \"{buildFolder}\" --no-goma");
            if (selectedOptions.Any())
            {
                sb.Append(" --");
                foreach (var selectedOption in selectedOptions)
                {
                    sb.Append(' ');
                    sb.Append(selectedOption.Name);
                    sb.Append('=');
                    sb.Append(selectedOption.DisplayValue);
                }
            }
            return sb.ToString();
        }

        public CommandHandler DownloadDepotTools { get; }
        public CommandHandler UpdateDepotTools { get; }
        public CommandHandler BrowseSource { get; }
        public CommandHandler DownloadSource { get; }
        public CommandHandler UpdateSource { get; }
        public CommandHandler GetOptionsAvailable { get; }
        public CommandHandler OpenReadMe { get; }
        public CommandHandler ExploreConfiguration { get; }
        public CommandHandler ConfigureBuild { get; }
        public CommandHandler BrowseBuildFolder { get; }
        public CommandHandler MoveAvailable { get; }
        public CommandHandler MoveSelected { get; }
        public CommandHandler BrowseWindowsKit { get; }
        public CommandHandler BuildV8 { get; }
        public string AvailableFilter { get; set; }
        public string SelectedFilter { get; set; }

        private int currentTab_;
        private string statusText_ = "Ready";
        private double progressValue_ = 0;
        public BuildOption describingOption_;
        private static char[] invalidFileNameCharacters_;
    }
}