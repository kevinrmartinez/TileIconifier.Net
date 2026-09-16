#region LICENCE

// /*
//         The MIT License (MIT)
// 
//         Copyright (c) 2021 Johnathon M
// 
//         Permission is hereby granted, free of charge, to any person obtaining a copy
//         of this software and associated documentation files (the "Software"), to deal
//         in the Software without restriction, including without limitation the rights
//         to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//         copies of the Software, and to permit persons to whom the Software is
//         furnished to do so, subject to the following conditions:
// 
//         The above copyright notice and this permission notice shall be included in
//         all copies or substantial portions of the Software.
// 
//         THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//         IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//         FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//         AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//         LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//         OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//         THE SOFTWARE.
// 
// */

#endregion

using System.Diagnostics;
using Newtonsoft.Json;
using TileIconifier.Core.Shortcut;

namespace TileIconifier.Core.Utilities
{
    public static class PowerShellUtils
    {
        private static void ExecutePowerShellCommand(string arguments, out string stdOutput)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("-NoProfile");
            // startInfo.ArgumentList.Add("-ExecutionPolicy");
            // startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-Command");
            startInfo.ArgumentList.Add(arguments);
            
            using var process = Process.Start(startInfo);
            if (process is null) throw new PowershellException();
            process.WaitForExit(2000);  // Time-out is kind of arbitrary... 
            stdOutput = process.StandardOutput.ReadToEnd();
            if (process.ExitCode != 0) throw new PowershellException();
        }
        
        public static void DumpStartLayout(string outputPath)
        {
            var arg = $"Export-StartLayout -Path '{outputPath}'";
            ExecutePowerShellCommand(arg, out _);

            if (!File.Exists(outputPath)) throw new PowershellException();
        }

        public static void MarryAppIDs(List<ShortcutItem>? shortcutsList)
        {
            if (shortcutsList == null)  return;

            var arg = "Get-StartApps | ConvertTo-Json";
            ExecutePowerShellCommand(arg, out var jsonOutput);
            
            if (string.IsNullOrEmpty(jsonOutput)) return;

            var startApps = JsonConvert.DeserializeObject<List<StartAppModel>>(jsonOutput);
            if (startApps is null) return;
            foreach (var properties in startApps)
            {
                var shortcutItem = shortcutsList.FirstOrDefault(s => 
                    Path.GetFileNameWithoutExtension(s.ShortcutFileInfo.Name) == properties.Name);
                shortcutItem?.AppId = properties.AppID;
            }
        }
    }
    
    file class StartAppModel
    {
        [JsonProperty(nameof(Name))]
        public string Name { get; set; } = string.Empty;
        [JsonProperty(nameof(AppID))]
        public string AppID { get; set; } = string.Empty;
    }
}