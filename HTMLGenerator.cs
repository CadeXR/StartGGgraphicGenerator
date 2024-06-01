using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace StartGGgraphicGenerator
{
    public static class HTMLGenerator
    {
        public static string GenerateHtmlContent(List<Player> players, string selectedFont, Color selectedColor)
        {
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
                <div class='players'>";

            int count = 0;
            int totalPlayers = players.Count;

            foreach (var player in players)
            {
                Color playerColor = CalculateGradientColor(selectedColor, Colors.Gray, count, totalPlayers);
                string colorHex = $"#{playerColor.R:X2}{playerColor.G:X2}{playerColor.B:X2}";

                if (count % 2 == 0)
                {
                    html += "<div class='player-row'>";
                }

                html += $"<div class='player-container'><div class='player-box' style='background-color: {colorHex};'>{player.Placement} - {player.Name}</div></div>";

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

        private static Color CalculateGradientColor(Color startColor, Color endColor, int step, int totalSteps)
        {
            byte r = (byte)(startColor.R + (endColor.R - startColor.R) * step / totalSteps);
            byte g = (byte)(startColor.G + (endColor.G - startColor.G) * step / totalSteps);
            byte b = (byte)(startColor.B + (endColor.B - startColor.B) * step / totalSteps);

            return Color.FromRgb(r, g, b);
        }
    }
}
