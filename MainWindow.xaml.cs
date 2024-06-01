using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;

namespace StartGGgraphicGenerator
{
    public partial class MainWindow : Window
    {
        private static string apiUrl = "https://api.start.gg/gql/alpha";
        private static string apiToken = "";
        private static List<Player> players = new List<Player>();
        private static List<Event> events = new List<Event>();
        private System.Windows.Media.Color selectedColor = System.Windows.Media.Colors.Blue;
        private string selectedFont = "Arial";
        private string netlifySiteId;
        private string netlifyAccessToken;
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "application.log");

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private async void AuthorizeButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Please enter your Netlify Site ID and Access Token.");
        }

        private async void FetchDataButton_Click(object sender, RoutedEventArgs e)
        {
            apiToken = ApiKeyTextBox.Text;
            string url = UrlTextBox.Text;
            string slug = ExtractSlugFromUrl(url);

            if (!string.IsNullOrEmpty(slug))
            {
                Log("Fetching events from tournament...");
                try
                {
                    await FetchEventsFromTournament(slug);
                    if (events.Count > 0)
                    {
                        Log($"Events fetched successfully. Found {events.Count} events.");
                        GenerateEventButtons();
                    }
                    else
                    {
                        Log("No events found.");
                        MessageBox.Show("No events found for this tournament.");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error fetching events: {ex.Message}");
                    MessageBox.Show($"Error fetching events: {ex.Message}");
                }
            }
            else
            {
                Log("Invalid URL entered.");
            }
        }

        private void GenerateEventButtons()
        {
            EventsStackPanel.Children.Clear();
            foreach (var evt in events)
            {
                var button = new Button
                {
                    Content = evt.Name,
                    Tag = evt.Id,
                    Margin = new Thickness(5)
                };
                button.Click += EventButton_Click;
                EventsStackPanel.Children.Add(button);
            }
        }

        private async void EventButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                string eventId = button.Tag.ToString();
                Log($"Fetching players from event: {button.Content}");
                try
                {
                    players = await FetchPlayersFromEvent(eventId);
                    if (players.Count > 0)
                    {
                        Log("Generating HTML file...");
                        var htmlContent = HTMLGenerator.GenerateHtmlContent(players, selectedFont, selectedColor);

                        // Save HTML file locally
                        var sourceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlayerData.html");
                        File.WriteAllText(sourceFilePath, htmlContent);

                        // Define the deploy directory
                        var deployDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deploy");

                        Log("Deploying HTML file to Netlify...");
                        try
                        {
                            string siteId = NetlifySiteIdTextBox.Text; // Get the site ID from the TextBox
                            await Task.Run(() => NetlifyDeployer.Deploy(sourceFilePath, deployDirectory, siteId));
                            Log("HTML file deployed successfully.");
                        }
                        catch (Exception ex)
                        {
                            Log($"Deployment failed: {ex.Message}");
                            MessageBox.Show($"Deployment failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        Log("No players found for this event.");
                        MessageBox.Show("No players found for this event.");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error fetching players: {ex.Message}");
                    MessageBox.Show($"Error fetching players: {ex.Message}");
                }
            }
        }



        private static string ExtractSlugFromUrl(string url)
        {
            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                string[] segments = uri.Segments;
                return segments.Length > 2 ? segments[2].TrimEnd('/') : null;
            }
            return null;
        }

        private async Task FetchEventsFromTournament(string slug)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

                var query = new
                {
                    query = @"
                    {
                        tournament(slug: """ + slug + @""") {
                            events {
                                id
                                name
                            }
                        }
                    }"
                };

                var jsonContent = JsonConvert.SerializeObject(query);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                var graphQLResponse = JsonConvert.DeserializeObject<GraphQLResponse>(result);

                if (graphQLResponse?.Data?.Tournament?.Events != null)
                {
                    events = graphQLResponse.Data.Tournament.Events;
                    Log($"Fetched {events.Count} events.");
                }
                else
                {
                    Log("No events found for this tournament.");
                    MessageBox.Show("No events found for this tournament.");
                }
            }
        }

        private async Task<List<Player>> FetchPlayersFromEvent(string eventId)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

                var query = new
                {
                    query = @"
                    {
                        event(id: """ + eventId + @""") {
                            standings(query: {perPage: 100, page: 1}) {
                                nodes {
                                    entrant {
                                        id
                                        name
                                    }
                                    placement
                                }
                            }
                        }
                    }"
                };

                var jsonContent = JsonConvert.SerializeObject(query);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                var graphQLResponse = JsonConvert.DeserializeObject<GraphQLResponse>(result);

                var playerList = new List<Player>();

                if (graphQLResponse?.Data?.Event?.Standings?.Nodes != null)
                {
                    foreach (var node in graphQLResponse.Data.Event.Standings.Nodes)
                    {
                        playerList.Add(new Player
                        {
                            Name = node.Entrant.Name,
                            Placement = node.Placement
                        });
                    }
                }

                return playerList;
            }
        }

        private void LoadSettings()
        {
            if (File.Exists("settings.txt"))
            {
                var lines = File.ReadAllLines("settings.txt");
                if (lines.Length >= 4)
                {
                    ApiKeyTextBox.Text = lines[0];
                    UrlTextBox.Text = lines[1];
                    NetlifySiteIdTextBox.Text = lines[2];
                    NetlifyAccessTokenBox.Password = lines[3];
                }
            }
        }

        private void SaveSettings()
        {
            var lines = new string[]
            {
                ApiKeyTextBox.Text,
                UrlTextBox.Text,
                NetlifySiteIdTextBox.Text,
                NetlifyAccessTokenBox.Password
            };
            File.WriteAllLines("settings.txt", lines);
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Text == textBox.Tag.ToString())
            {
                textBox.Text = "";
                textBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = textBox.Tag.ToString();
                textBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null && passwordBox.Password == passwordBox.Tag.ToString())
            {
                passwordBox.Password = "";
                passwordBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null && string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                passwordBox.Password = passwordBox.Tag.ToString();
                passwordBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void NetlifyAccessTokenBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null)
            {
                netlifyAccessToken = passwordBox.Password;
            }
        }

        private void ColorPickerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ColorPickerComboBox.SelectedItem != null)
            {
                var selectedColorString = (ColorPickerComboBox.SelectedItem as ComboBoxItem).Tag.ToString();
                selectedColor = (Color)ColorConverter.ConvertFromString(selectedColorString);
            }
        }

        private void Log(string message)
        {
            LogToFile(message);
            LogTextBox.AppendText(message + Environment.NewLine);
            LogTextBox.ScrollToEnd();
        }

        private static void LogToFile(string message)
        {
            try
            {
                File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            MessageBox.Show("Settings saved successfully.");
        }
    }

    public class Player
    {
        public string Name { get; set; }
        public int Placement { get; set; }
    }

    public class GraphQLResponse
    {
        public Data Data { get; set; }
    }

    public class Data
    {
        public Event Event { get; set; }
        public Tournament Tournament { get; set; }
    }

    public class Event
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Standings Standings { get; set; }
    }

    public class Standings
    {
        public List<Node> Nodes { get; set; }
    }

    public class Node
    {
        public Entrant Entrant { get; set; }
        public int Placement { get; set; }
    }

    public class Entrant
    {
        public string Name { get; set; }
    }

    public class Tournament
    {
        public List<Event> Events { get; set; }
    }
}
