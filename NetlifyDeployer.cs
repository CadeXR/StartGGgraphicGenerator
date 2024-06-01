using System;
using System.Diagnostics;
using System.IO;

namespace StartGGgraphicGenerator
{
    public static class NetlifyDeployer
    {
        public static void Deploy(string sourceFilePath, string deployDirectory)
        {
            try
            {
                string nodePath = GetNodeJsPath();
                if (string.IsNullOrEmpty(nodePath))
                {
                    throw new Exception("Node.js not found. Please install it from https://nodejs.org/");
                }

                string netlifyCliPath = GetNetlifyCliPath();
                if (string.IsNullOrEmpty(netlifyCliPath))
                {
                    throw new Exception("Netlify CLI not found. Please install it from https://docs.netlify.com/cli/get-started/");
                }

                // Create the deploy directory if it doesn't exist
                if (!Directory.Exists(deployDirectory))
                {
                    Directory.CreateDirectory(deployDirectory);
                }

                Console.WriteLine($"Deploy directory created at: {deployDirectory}");

                // Copy and rename the HTML file to index.html in the deploy directory
                string destinationFilePath = Path.Combine(deployDirectory, "index.html");
                File.Copy(sourceFilePath, destinationFilePath, true);

                Console.WriteLine($"File copied to: {destinationFilePath}");

                // Include the Node.js directory in the PATH environment variable
                string nodeDirectory = Path.GetDirectoryName(nodePath);
                string originalPath = Environment.GetEnvironmentVariable("PATH");
                string newPath = $"{nodeDirectory};{originalPath}";

                // Deploy the directory using Netlify CLI
                ProcessStartInfo deployStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C cd /d \"{deployDirectory}\" && \"{netlifyCliPath}\" deploy --dir . --prod",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    EnvironmentVariables = { ["PATH"] = newPath } // Set the updated PATH
                };

                using (Process deployProcess = new Process { StartInfo = deployStartInfo })
                {
                    deployProcess.Start();
                    string output = deployProcess.StandardOutput.ReadToEnd();
                    string error = deployProcess.StandardError.ReadToEnd();
                    deployProcess.WaitForExit();

                    Console.WriteLine(output);
                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception($"Error during deployment: {error}");
                    }

                    Console.WriteLine("Deployment completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during deployment: {ex.Message}");
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
                Console.WriteLine($"Failed to locate Node.js: {ex.Message}");
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
                Console.WriteLine($"Failed to locate Netlify CLI: {ex.Message}");
            }

            return null;
        }
    }
}
