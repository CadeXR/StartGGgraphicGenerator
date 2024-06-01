using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;

namespace StartGGgraphicGenerator
{
    public static class HTMLGenerator
    {
        public static string GenerateHtmlContent(List<Player> players, string selectedFont, Color selectedColor, string imagePath)
        {
            string colorHexStart = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";
            string colorHexEnd = "#808080"; // Grey color
            int playerCount = players.Count;

            string gradientStyles = GenerateGradientStyles(playerCount, colorHexStart, colorHexEnd);

            string html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: {selectedFont}; background-color: white; text-align: center; }}
                    .player-container {{ display: inline-block; width: 50%; }}
                    .player-box {{ padding: 20px; margin: 10px; border-radius: 10px; }}
                    {gradientStyles}
                </style>
            </head>
            <body>
                {(string.IsNullOrEmpty(imagePath) ? "" : $"<img src='{Path.GetFileName(imagePath)}' width='200' height='200' alt='Image' />")}
                <div class='players'>";

            int count = 0;
            foreach (var player in players)
            {
                if (count % 2 == 0)
                {
                    html += "<div class='player-row'>";
                }

                html += $"<div class='player-container'><div class='player-box gradient-{count + 1}'>{player.Placement} - {player.Name}</div></div>";

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

            if (!string.IsNullOrEmpty(imagePath))
            {
                string destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deploy", Path.GetFileName(imagePath));
                File.Copy(imagePath, destinationPath, true);
            }

            return html;
        }

        private static string GenerateGradientStyles(int playerCount, string startColor, string endColor)
        {
            var styles = new StringBuilder();

            for (int i = 0; i < playerCount; i++)
            {
                double ratio = (double)i / (playerCount - 1);
                string gradientColor = InterpolateColor(startColor, endColor, ratio);
                styles.AppendLine($".gradient-{i + 1} {{ background-color: {gradientColor}; }}");
            }

            return styles.ToString();
        }

        private static string InterpolateColor(string startColor, string endColor, double ratio)
        {
            int r1 = Convert.ToInt32(startColor.Substring(1, 2), 16);
            int g1 = Convert.ToInt32(startColor.Substring(3, 2), 16);
            int b1 = Convert.ToInt32(startColor.Substring(5, 2), 16);

            int r2 = Convert.ToInt32(endColor.Substring(1, 2), 16);
            int g2 = Convert.ToInt32(endColor.Substring(3, 2), 16);
            int b2 = Convert.ToInt32(endColor.Substring(5, 2), 16);

            int r = (int)(r1 + (r2 - r1) * ratio);
            int g = (int)(g1 + (g2 - g1) * ratio);
            int b = (int)(b1 + (b2 - b1) * ratio);

            return $"#{r:X2}{g:X2}{b:X2}";
        }
    }
}
