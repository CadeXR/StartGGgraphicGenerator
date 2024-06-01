using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace StartGGgraphicGenerator
{
    public static class HTMLGenerator
    {
        public static string GenerateHtmlContent(List<Player> players, string selectedFont, Color selectedColor, string imagePath)
        {
            string colorHex = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";
            string greyHex = "#808080";

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
            <body>";

            if (!string.IsNullOrEmpty(imagePath))
            {
                string base64Image = Convert.ToBase64String(File.ReadAllBytes(imagePath));
                string imgSrc = $"data:image/png;base64,{base64Image}";
                html += $"<img src='{imgSrc}' alt='Dropped Image' style='display: block; margin-left: auto; margin-right: auto;'/>";
            }

            html += "<div class='players'>";
            int count = 0;
            foreach (var player in players)
            {
                Color boxColor = InterpolateColor(selectedColor, Colors.Gray, (float)count / (players.Count - 1));
                string boxHex = $"#{boxColor.R:X2}{boxColor.G:X2}{boxColor.B:X2}";
                html += $"<div class='player-container'><div class='player-box' style='background-color: {boxHex};'>{player.Placement} - {player.Name}</div></div>";
                count++;
            }
            html += "</div></body></html>";

            return html;
        }

        private static Color InterpolateColor(Color start, Color end, float factor)
        {
            byte r = (byte)(start.R + (end.R - start.R) * factor);
            byte g = (byte)(start.G + (end.G - start.G) * factor);
            byte b = (byte)(start.B + (end.B - start.B) * factor);
            return Color.FromRgb(r, g, b);
        }
    }
}
