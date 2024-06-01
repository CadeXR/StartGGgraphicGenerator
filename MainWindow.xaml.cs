using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;

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
        private static string logFilePath = "log.txt";
        private string droppedImagePath;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private async void FetchDataButton_Click(object sender, RoutedEventArgs e)
        {
            apiToken = ApiKeyTextBox.Text;
            string url = UrlTextBox.Text;
            string slug = ExtractSlugFromUrl(url);
            bool isLeague = url.Contains("/league/");

            if (!string.IsNullOrEmpty(slug))
            {
                if (isLeague)
                {
                    Log("Fetching events from league...");
                    await FetchEventsFromLeague(slug);
                }
                else
                {
                    Log("Fetching events from tournament...");
                    await FetchEventsFromTournament(slug);
                }

                if (events.Count > 0)
                {
                    Log($"Events fetched successfully. Found {events.Count} events.");
                    GenerateEventButtons();
                }
                else
                {
                    Log("No events found.");
                    MessageBox.Show("No events found for this tournament or league.");
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
                players = await FetchPlayersFromEvent(eventId);
                if (players.Count > 0)
                {
                    Log("Generating HTML file...");
                    var htmlContent = HTMLGenerator.GenerateHtmlContent(players, selectedFont, selectedColor, droppedImagePath);

                    // Save HTML file locally
                    var sourceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlayerData.html");
                    File.WriteAllText(sourceFilePath, htmlContent);

                    // Define the deploy directory
                    var deployDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deploy");

                    Log("Deploying HTML file to Netlify...");
                    try
                    {
                        NetlifyDeployer.Deploy(sourceFilePath, deployDirectory, netlifySiteId);
                        Log("HTML file deployed successfully.");
                    }
                    catch (Exception ex)
                    {
                        Log($"Deployment failed: {ex.Message}");
                    }
                }
                else
                {
                    Log("No players found for this event.");
                    MessageBox.Show("No players found for this event.");
                }
            }
        }

        private async void FetchStandingsButton_Click(object sender, RoutedEventArgs e)
        {
            apiToken = ApiKeyTextBox.Text;
            string url = UrlTextBox.Text;
            string slug = ExtractSlugFromUrl(url);
            bool isLeague = url.Contains("/league/");

            if (!string.IsNullOrEmpty(slug))
            {
                if (isLeague)
                {
                    Log("Fetching standings from league...");
                    await FetchStandingsFromLeague(slug);
                }
                else
                {
                    Log("Fetching standings from tournament...");
                    await FetchStandingsFromTournament(slug);
                }

                if (players.Count > 0)
                {
                    Log("Generating HTML file...");
                    var htmlContent = HTMLGenerator.GenerateHtmlContent(players, selectedFont, selectedColor, droppedImagePath);

                    // Save HTML file locally
                    var sourceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlayerData.html");
                    File.WriteAllText(sourceFilePath, htmlContent);

                    // Define the deploy directory
                    var deployDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deploy");

                    Log("Deploying HTML file to Netlify...");
                    try
                    {
                        NetlifyDeployer.Deploy(sourceFilePath, deployDirectory, netlifySiteId);
                        Log("HTML file deployed successfully.");
                    }
                    catch (Exception ex)
                    {
                        Log($"Deployment failed: {ex.Message}");
                    }
                }
                else
                {
                    Log("No players found for this league or tournament.");
                    MessageBox.Show("No players found for this league or tournament.");
                }
            }
            else
            {
                Log("Invalid URL entered.");
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

        private async Task FetchEventsFromLeague(string slug)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

                var query = new
                {
                    query = @"
                    {
                        league(slug: """ + slug + @""") {
                            events {
                                nodes {
                                    id
                                    name
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

                if (graphQLResponse?.Data?.League?.Events?.Nodes != null)
                {
                    events = graphQLResponse.Data.League.Events.Nodes;
                    Log($"Fetched {events.Count} events.");
                }
                else
                {
                    Log("No events found for this league.");
                    MessageBox.Show("No events found for this league.");
                }
            }
        }

        private async Task FetchStandingsFromTournament(string slug)
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
                        }
                    }"
                };

                var jsonContent = JsonConvert.SerializeObject(query);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                var result = await response.Content.ReadAsStringAsync();
                Log($"Tournament standings response: {result}");
                response.EnsureSuccessStatusCode();

                var graphQLResponse = JsonConvert.DeserializeObject<GraphQLResponse>(result);

                players.Clear();

                if (graphQLResponse?.Data?.Tournament?.Events != null)
                {
                    foreach (var evt in graphQLResponse.Data.Tournament.Events)
                    {
                        if (evt.Standings?.Nodes != null)
                        {
                            foreach (var node in evt.Standings.Nodes)
                            {
                                players.Add(new Player
                                {
                                    Name = node.Entrant.Name,
                                    Placement = node.Placement
                                });
                            }
                        }
                    }
                    Log($"Fetched standings from {graphQLResponse.Data.Tournament.Events.Count} events.");
                }
                else
                {
                    Log("No standings found for this tournament.");
                    MessageBox.Show("No standings found for this tournament.");
                }
            }
        }

        private async Task FetchStandingsFromLeague(string slug)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

                var query = new
                {
                    query = @"
                    {
                        league(slug: """ + slug + @""") {
                            events {
                                nodes {
                                    id
                                    name
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
                            }
                        }
                    }"
                };

                var jsonContent = JsonConvert.SerializeObject(query);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                var result = await response.Content.ReadAsStringAsync();
                Log($"League standings response: {result}");
                response.EnsureSuccessStatusCode();

                var graphQLResponse = JsonConvert.DeserializeObject<GraphQLResponse>(result);

                players.Clear();

                if (graphQLResponse?.Data?.League?.Events?.Nodes != null)
                {
                    foreach (var evt in graphQLResponse.Data.League.Events.Nodes)
                    {
                        if (evt.Standings?.Nodes != null)
                        {
                            foreach (var node in evt.Standings.Nodes)
                            {
                                players.Add(new Player
                                {
                                    Name = node.Entrant.Name,
                                    Placement = node.Placement
                                });
                            }
                        }
                    }
                    Log($"Fetched standings from {graphQLResponse.Data.League.Events.Nodes.Count} events.");
                }
                else
                {
                    Log("No standings found for this league.");
                    MessageBox.Show("No standings found for this league.");
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
                var result = await response.Content.ReadAsStringAsync();
                Log($"Event players response: {result}");
                response.EnsureSuccessStatusCode();

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
            Log("Settings saved.");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void NetlifyAccessTokenBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null)
            {
                netlifyAccessToken = passwordBox.Password;
            }
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (ColorPicker.SelectedColor.HasValue)
            {
                selectedColor = ColorPicker.SelectedColor.Value;
            }
        }

        private void Log(string message)
        {
            string logMessage = $"{DateTime.Now}: {message}{Environment.NewLine}";
            LogTextBox.AppendText(logMessage);
            LogTextBox.ScrollToEnd();
            File.AppendAllText(logFilePath, logMessage);
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    droppedImagePath = files[0];
                    DroppedImage.Source = new BitmapImage(new Uri(droppedImagePath));
                    Log($"Image dropped: {droppedImagePath}");
                }
            }
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
        public League League { get; set; }
    }

    public class Event
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Standings Standings { get; set; }
    }

    public class Tournament
    {
        public List<Event> Events { get; set; }
    }

    public class League
    {
        public EventConnection Events { get; set; }
    }

    public class EventConnection
    {
        public List<Event> Nodes { get; set; }
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
}

