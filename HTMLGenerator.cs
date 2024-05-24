using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows;

namespace StartGGgraphicGenerator
{
    public static class HTMLGenerator
    {
        public static async void SaveHtmlToFile(List<Player> players, string selectedFont, System.Windows.Media.Color selectedColor, string githubUsername, string githubRepository, string githubToken)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlayerData.html");
                var htmlContent = GenerateHtmlContent(players, selectedFont, selectedColor);
                File.WriteAllText(filePath, htmlContent);
                MessageBox.Show($"Player data saved to {filePath}");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                // Push to GitHub
                await PushToGitHub(htmlContent, githubUsername, githubRepository, githubToken);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private static string GenerateHtmlContent(List<Player> players, string selectedFont, System.Windows.Media.Color selectedColor)
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

        private static async Task PushToGitHub(string content, string username, string repository, string token)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("request");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);

                    var fileContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
                    var message = new
                    {
                        message = "Automatic update from StartGGgraphicGenerator",
                        committer = new
                        {
                            name = username,
                            email = $"{username}@users.noreply.github.com"
                        },
                        content = fileContent
                    };

                    var jsonContent = JsonConvert.SerializeObject(message);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var url = $"https://api.github.com/repos/{username}/{repository}/contents/PlayerData.html";
                    var response = await client.PutAsync(url, httpContent);

                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Failed to push to GitHub: {response.StatusCode} - {responseContent}");
                    }

                    MessageBox.Show("Pushed to GitHub successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while pushing to GitHub: {ex.Message}");
            }
        }
    }
}
