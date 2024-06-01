using System;
using System.Diagnostics;
using System.IO;

namespace StartGGgraphicGenerator
{
    public static class NetlifyDeployer
    {
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "application.log");

        public static void Deploy(string sourceFilePath, string deployDirectory, string siteId)
        {
            try
            {
                Log("Starting deployment process...");

                string nodePath = GetNodeJsPath();
                if (string.IsNullOrEmpty(nodePath))
                {
                    throw new Exception("Node.js not found. Please install it from https://nodejs.org/");
                }
                Log($"Node.js found at: {nodePath}");

                string netlifyCliPath = GetNetlifyCliPath();
                if (string.IsNullOrEmpty(netlifyCliPath))
                {
                    throw new Exception("Netlify CLI not found. Please install it from https://docs.netlify.com/cli/get-started/");
                }
                Log($"Netlify CLI found at: {netlifyCliPath}");

                // Create the deploy directory if it doesn't exist
                if (!Directory.Exists(deployDirectory))
                {
                    Directory.CreateDirectory(deployDirectory);
                }

                Log($"Deploy directory created at: {deployDirectory}");

                // Copy and rename the HTML file to index.html in the deploy directory
                string destinationFilePath = Path.Combine(deployDirectory, "index.html");
                File.Copy(sourceFilePath, destinationFilePath, true);

                Log($"File copied to: {destinationFilePath}");

                // Include the Node.js directory in the PATH environment variable
                string nodeDirectory = Path.GetDirectoryName(nodePath);
                string originalPath = Environment.GetEnvironmentVariable("PATH");
                string newPath = $"{nodeDirectory};{originalPath}";

                // Link the directory to the Netlify site
                ProcessStartInfo linkStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C cd /d \"{deployDirectory}\" && \"{netlifyCliPath}\" link --id {siteId}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    EnvironmentVariables = { ["PATH"] = newPath }
                };

                Log("Linking directory to Netlify site...");

                using (Process linkProcess = new Process { StartInfo = linkStartInfo })
                {
                    linkProcess.OutputDataReceived += (sender, args) => Log(args.Data);
                    linkProcess.ErrorDataReceived += (sender, args) => Log(args.Data);

                    linkProcess.Start();
                    linkProcess.BeginOutputReadLine();
                    linkProcess.BeginErrorReadLine();
                    linkProcess.WaitForExit();

                    Log("Netlify linking process completed.");
                    if (linkProcess.ExitCode != 0)
                    {
                        throw new Exception($"Netlify linking process exited with code {linkProcess.ExitCode}");
                    }
                }

                // Deploy the directory using Netlify CLI
                ProcessStartInfo deployStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C cd /d \"{deployDirectory}\" && \"{netlifyCliPath}\" deploy --dir . --prod",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    EnvironmentVariables = { ["PATH"] = newPath }
                };

                Log("Starting Netlify deployment process...");

                using (Process deployProcess = new Process { StartInfo = deployStartInfo })
                {
                    deployProcess.OutputDataReceived += (sender, args) => Log(args.Data);
                    deployProcess.ErrorDataReceived += (sender, args) => Log(args.Data);

                    deployProcess.Start();
                    deployProcess.BeginOutputReadLine();
                    deployProcess.BeginErrorReadLine();
                    deployProcess.WaitForExit();

                    Log("Netlify deployment process completed.");
                    if (deployProcess.ExitCode != 0)
                    {
                        throw new Exception($"Netlify deployment process exited with code {deployProcess.ExitCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"An error occurred during deployment: {ex.Message}");
                throw;
            }
        }

        private static string GetNodeJsPath()
        {
            string[] possiblePaths =
            {
                @"C:\Program Files\nodejs\node.exe",
                @"C:\Program Files\nodejs\node.cmd",
                @"C:\Users\" + Environment.UserName + @"\AppData\Roaming\npm\node.exe",
                @"C:\Users\" + Environment.UserName + @"\AppData\Roaming\npm\node.cmd"
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/C where node",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                    {
                        string[] paths = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        return paths[0]; // Return the first found path
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to locate Node.js: {ex.Message}");
            }

            return null;
        }

        private static string GetNetlifyCliPath()
        {
            string[] possiblePaths =
            {
                @"C:\Program Files\nodejs\netlify.cmd",
                @"C:\Program Files\nodejs\netlify.exe",
                @"C:\Users\" + Environment.UserName + @"\AppData\Roaming\npm\netlify.cmd",
                @"C:\Users\" + Environment.UserName + @"\AppData\Roaming\npm\netlify.exe"
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/C where netlify",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                    {
                        string[] paths = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        return paths[0]; // Return the first found path
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to locate Netlify CLI: {ex.Message}");
            }

            return null;
        }

        private static void Log(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                try
                {
                    File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }
        }
    }
}
