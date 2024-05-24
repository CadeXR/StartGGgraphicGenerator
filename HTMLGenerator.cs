using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace StartGGgraphicGenerator
{
    public static class HTMLGenerator
    {
        public static void SaveHtmlToFile(List<Player> players, string selectedFont, Color selectedColor, string githubUsername, string githubRepository, string githubToken)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlayerData.html");

                var htmlContent = GenerateHtmlContent(players, selectedFont, selectedColor);

                File.WriteAllText(filePath, htmlContent);

                UploadToGitHub(filePath, githubUsername, githubRepository, githubToken);

                System.Windows.MessageBox.Show($"Player data saved to {filePath}");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private static string GenerateHtmlContent(List<Player> players, string selectedFont, Color selectedColor)
        {
            string colorHex = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";
            string html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: {selectedFont}; background-color: white; text-align: center; }}
                    .player-container {{ display: inline-block; width: 50%; }}
                    .player-box {{ background-color: {colorHex}; color: white; padding: 20px; margin: 10px; border-radius: 10px; }}
                </style>
            </head>
            <body>
                <div class='players'>";
            int count = 0;
            foreach (var player in players)
            {
                if (count % 2 == 0)
                {
                    html += "<div class='player-row'>";
                }

                html += $"<div class='player-container'><div class='player-box'>{player.Placement} - {player.Name}</div></div>";

                if (count % 2 == 1)
                {
                    html += "</div>";
                }

                count++;
            }

            if (count % 2 == 1)
            {
                html += "</div>";
            }

            html += @"
                </div>
            </body>
            </html>";
            return html;
        }

        private static async void UploadToGitHub(string filePath, string username, string repository, string token)
        {
            try
            {
                string url = $"https://api.github.com/repos/{username}/{repository}/contents/{Path.GetFileName(filePath)}";
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Mozilla", "5.0"));

                var content = new
                {
                    message = "Upload Player Data",
                    content = Convert.ToBase64String(File.ReadAllBytes(filePath))
                };

                var jsonContent = JsonConvert.SerializeObject(content);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(url, httpContent);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred while uploading to GitHub: {ex.Message}");
            }
        }
    }
}
