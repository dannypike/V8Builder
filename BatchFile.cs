using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Text;
using System.Threading.Tasks;

namespace V8Builder
{
    internal class BatchFile
    {
        public MainViewModel Model { get; set; }
        public string WorkingDirectory { get; set; }
        public string Title { get; set; } = "Doing something; not quite sure what it is, though";
        public IList<string> Commands { get; } = new List<string>();
        public ProcessStartInfo Info { get; }

        public BatchFile()
        {
            Info = new ProcessStartInfo
            {
                ErrorDialog = true,
                FileName = Environment.ExpandEnvironmentVariables("%COMSPEC%"),
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Normal,                
            };

        }

        public string PythonFileName
        {
            get
            {
                return pythonFileName_ ?? (pythonFileName_ = FindPython());
            }
        }

        public async Task<string> Run()
        {
            var batchFileName = Path.Combine((Application.Current as App).ConfigFolder, "run.cmd");
            if (!Directory.Exists(WorkingDirectory))
            {
                return $"directory not found: {WorkingDirectory}";
            }
            Info.WorkingDirectory = WorkingDirectory;
            Info.Arguments = $"/k {batchFileName}";

            Info.EnvironmentVariables.Remove("PATH");
            Info.EnvironmentVariables["WINDOWSDKDIR"] = Model.Config.WindowsKitFolder;

            var sb = new StringBuilder(Model.Config.DepotToolsFolder);
            var windir = Environment.ExpandEnvironmentVariables("%windir%");
            var existingPath = Environment.ExpandEnvironmentVariables("%PATH%").Split(';');
            foreach (var folder in existingPath)
            {
                var expanded = Environment.ExpandEnvironmentVariables(folder);
                if (expanded.StartsWith(windir, StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append(';');
                    sb.Append(folder);
                }
            }
            Info.EnvironmentVariables.Add("PATH", sb.ToString());

            File.WriteAllText(batchFileName, BuildCommands());
            await Task.Run(() => Process.Start(Info).WaitForExit());
            return "Ready";
        }

        private string BuildCommands()
        {
            var sb = new StringBuilder();
            sb.Append($"@echo off\r\n");
            sb.Append($"title {Title}\r\n");
            foreach (var command in Commands)
            {
                sb.Append($"echo ==================================================\r\n");
                sb.Append($"echo.\r\n");
                sb.Append($"echo running '{command}'\r\n");
                sb.Append($"echo.\r\n");
                sb.Append(command);
                sb.Append("\r\n");
            }
            sb.Append($"title Please close this window before continuing with V8Builder\r\n");
            sb.Append($"echo.\r\n");
            sb.Append($"echo Please close this window before continuing with V8Builder\r\n");
            return sb.ToString();
        }

        private string FindPython()
        {
            var configFolder = (Application.Current as App).ConfigFolder;
            return pythonFileName_ = Directory.EnumerateFiles(configFolder, "python.exe", SearchOption.AllDirectories)
                .FirstOrDefault();
        }

        private string pythonFileName_;
    }
}
