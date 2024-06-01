using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace StartGGgraphicGenerator
{
    public static class HTMLGenerator
    {
        public static string GenerateHtmlContent(List<Player> players, string selectedFont, Color selectedColor, string imagePath)
        {
            string colorHex = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";
            string gradientColorHex = "#808080"; // Grey color

            // Calculate gradient steps
            int playerCount = players.Count;
            double stepR = (128 - selectedColor.R) / (double)playerCount;
            double stepG = (128 - selectedColor.G) / (double)playerCount;
            double stepB = (128 - selectedColor.B) / (double)playerCount;

            string imageHtml = string.Empty;
            if (!string.IsNullOrEmpty(imagePath))
            {
                string imageBase64 = Convert.ToBase64String(File.ReadAllBytes(imagePath));
                imageHtml = $"<img src='data:image/png;base64,{imageBase64}' width='200' height='200' style='display: block; margin-left: auto; margin-right: auto;' />";
            }

            string html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: {selectedFont}; background-color: white; text-align: center; }}
                    .player-container {{ display: inline-block; width: 50%; }}
                    .player-box {{ color: white; padding: 20px; margin: 10px; border-radius: 10px; }}
                </style>
            </head>
            <body>
                {imageHtml}
                <div class='players'>";
            int count = 0;
            foreach (var player in players)
            {
                if (count % 2 == 0)
                {
                    html += "<div class='player-row'>";
                }

                // Calculate current gradient color
                int currentR = (int)(selectedColor.R + stepR * count);
                int currentG = (int)(selectedColor.G + stepG * count);
                int currentB = (int)(selectedColor.B + stepB * count);
                string currentColorHex = $"#{currentR:X2}{currentG:X2}{currentB:X2}";

                html += $"<div class='player-container'><div class='player-box' style='background-color: {currentColorHex};'>{player.Placement} - {player.Name}</div></div>";

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
