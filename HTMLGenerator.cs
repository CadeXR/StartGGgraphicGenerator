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
    }
}
