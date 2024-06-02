using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using static StartGGgraphicGenerator.MainWindow;

namespace StartGGgraphicGenerator
{
    public static class HTMLGenerator
    {
        public static string GenerateHtmlContent(List<Player> players, string selectedFont, Color selectedColor, string imagePath)
        {
            string html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: {selectedFont}; background-color: white; text-align: center; }}
                    .players {{ display: flex; flex-wrap: wrap; justify-content: center; }}
                    .player-container {{ flex: 0 0 45%; margin: 10px; box-sizing: border-box; }}
                    .player-box {{ color: white; padding: 20px; margin: 10px; border-radius: 10px; }}
                    .player-info {{ display: flex; justify-content: space-between; }}
                </style>
            </head>
            <body>
                <div class='players'>";

            if (!string.IsNullOrEmpty(imagePath))
            {
                string imageBase64 = Convert.ToBase64String(File.ReadAllBytes(imagePath));
                string imageSrc = $"data:image/png;base64,{imageBase64}";
                html += $"<img src='{imageSrc}' alt='Dropped Image' style='display: block; margin-left: auto; margin-right: auto;' />";
            }

            int totalPlayers = players.Count;
            for (int i = 0; i < totalPlayers; i++)
            {
                double gradientStep = (double)i / (totalPlayers - 1);
                byte r = (byte)(selectedColor.R + gradientStep * (128 - selectedColor.R));
                byte g = (byte)(selectedColor.G + gradientStep * (128 - selectedColor.G));
                byte b = (byte)(selectedColor.B + gradientStep * (128 - selectedColor.B));
                string gradientColorHex = $"#{r:X2}{g:X2}{b:X2}";

                html += $@"
                <div class='player-container'>
                    <div class='player-box' style='background-color: {gradientColorHex};'>
                        <div class='player-info'>
                            <span>{players[i].Placement}. {players[i].Name}</span>
                            <span>{players[i].Points}</span>
                        </div>
                    </div>
                </div>";

                if (i % 2 == 1)
                {
                    html += "<div style='clear: both;'></div>";
                }
            }

            html += @"
                </div>
            </body>
            </html>";

            return html;
        }
    }
}
