using System;
using System.Collections.Generic;
using System.IO;

namespace StartGGgraphicGenerator
{
    public static class HTMLGenerator
    {
        public static void SaveHtmlToFile(List<Player> players, string selectedFont, string selectedColor)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlayerData.html");

                var htmlContent = GenerateHtmlContent(players, selectedFont, selectedColor);

                File.WriteAllText(filePath, htmlContent);
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

        private static string GenerateHtmlContent(List<Player> players, string selectedFont, string selectedColor)
        {
            string html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: {selectedFont}; background-color: white; text-align: center; }}
                    .player-container {{ display: inline-block; width: 50%; }}
                    .player-box {{ background-color: {selectedColor}; color: white; padding: 20px; margin: 10px; border-radius: 10px; }}
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
    }
}
