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
using System.Diagnostics;
using System.IO.Compression;
using System.Numerics;
using System.Windows.Threading;

namespace StartGGgraphicGenerator
{
    public partial class MainWindow : Window
    {
        private static string apiUrl = "https://api.start.gg/gql/alpha";
        private static string apiToken = "";
        private static List<Player> players = new List<Player>();
        private static List<Event> events = new List<Event>();
        private Color selectedColor = Colors.Blue;
        private string selectedFont = "Arial";
        private string netlifySiteId;
        private string netlifyAccessToken;
        private static string logFilePath = "log.txt";
        private string droppedImagePath;
        private DispatcherTimer serverModeTimer;
        private bool isServerModePrimed = false;
        private bool isServerModeActive = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            InitializeServerMode();
        }

        private void InitializeServerMode()
        {
            serverModeTimer = new DispatcherTimer();
            serverModeTimer.Interval = TimeSpan.FromSeconds(30);
            serverModeTimer.Tick += ServerModeTimer_Tick;
        }

        private void ServerModeTimer_Tick(object sender, EventArgs e)
        {
            Log("Server Mode: Generating and pushing graphic...");
            FetchStandingsButton_Click(null, null);
        }

        private void ServerModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            isServerModePrimed = true;
        }

        private void ServerModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            isServerModePrimed = false;
            isServerModeActive = false;
            serverModeTimer.Stop();
            HideServerModeOverlay();
        }

        private void ShowServerModeOverlay()
        {
            ServerModeOverlay.Visibility = Visibility.Visible;
        }

        private void HideServerModeOverlay()
        {
            ServerModeOverlay.Visibility = Visibility.Collapsed;
        }

        private async void FetchDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (isServerModePrimed && isServerModeActive)
            {
                return;
            }

            apiToken = ApiKeyTextBox.Text;
            string url = UrlTextBox.Text;
            string slug = ExtractSlugFromUrl(url);
            string linkType = (LinkTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            bool isLeague = linkType == "League";

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
            if (isServerModePrimed && isServerModeActive)
            {
                return;
            }

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

                    var sourceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlayerData.html");
                    File.WriteAllText(sourceFilePath, htmlContent);

                    if (PushToServerCheckBox.IsChecked == true)
                    {
                        var deployDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deploy");
                        if (Directory.Exists(deployDirectory))
                        {
                            Directory.Delete(deployDirectory, true);
                        }
                        Directory.CreateDirectory(deployDirectory);

                        var destinationFilePath = Path.Combine(deployDirectory, "index.html");
                        File.Copy(sourceFilePath, destinationFilePath, true);

                        Log("Deploying HTML file to Netlify...");
                        try
                        {
                            NetlifyDeployer.Deploy(deployDirectory, netlifySiteId, netlifyAccessToken);
                            Log("HTML file deployed successfully.");
                        }
                        catch (Exception ex)
                        {
                            Log($"Deployment failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = sourceFilePath,
                            UseShellExecute = true
                        });
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
            if (isServerModePrimed)
            {
                isServerModeActive = true;
                ShowServerModeOverlay();
                serverModeTimer.Start();
            }

            apiToken = ApiKeyTextBox.Text;
            string url = UrlTextBox.Text;
            string slug = ExtractSlugFromUrl(url);
            string linkType = (LinkTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            bool isLeague = linkType == "League";

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

                    var sourceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlayerData.html");
                    File.WriteAllText(sourceFilePath, htmlContent);

                    if (PushToServerCheckBox.IsChecked == true)
                    {
                        var deployDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deploy");
                        if (Directory.Exists(deployDirectory))
                        {
                            Directory.Delete(deployDirectory, true);
                        }
                        Directory.CreateDirectory(deployDirectory);

                        var destinationFilePath = Path.Combine(deployDirectory, "index.html");
                        File.Copy(sourceFilePath, destinationFilePath, true);

                        Log("Deploying HTML file to Netlify...");
                        try
                        {
                            NetlifyDeployer.Deploy(deployDirectory, netlifySiteId, netlifyAccessToken);
                            Log("HTML file deployed successfully.");
                        }
                        catch (Exception ex)
                        {
                            Log($"Deployment failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = sourceFilePath,
                            UseShellExecute = true
                        });
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
                                standings(query: {perPage: 100, page: 1}) {
                                    nodes {
                                        entrant {
                                            id
                                            name
                                        }
                                        placement
                                        totalPoints
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
                                    Placement = node.Placement,
                                    Points = node.TotalPoints
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
                                    standings(query: {perPage: 100, page: 1}) {
                                        nodes {
                                            entrant {
                                                id
                                                name
                                            }
                                            placement
                                            totalPoints
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
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                var graphQLResponse = JsonConvert.DeserializeObject<GraphQLResponse>(result);

                events.Clear();

                if (graphQLResponse?.Data?.League?.Events?.Nodes != null)
                {
                    foreach (var evt in graphQLResponse.Data.League.Events.Nodes)
                    {
                        events.Add(new Event
                        {
                            Id = evt.Id,
                            Name = evt.Name,
                            Standings = evt.Standings
                        });
                    }
                    Log($"Fetched {events.Count} events.");
                }
                else
                {
                    Log("No events found for this league.");
                    MessageBox.Show("No events found for this league.");
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
                    standings(query: {perPage: 100, page: 1}) {
                        nodes {
                            player {
                                prefix
                                gamerTag
                            }
                            placement
                            totalPoints
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

                if (graphQLResponse?.Data?.League?.Standings?.Nodes != null)
                {
                    foreach (var node in graphQLResponse.Data.League.Standings.Nodes)
                    {
                        players.Add(new Player
                        {
                            Name = node.Player.Prefix + " " + node.Player.GamerTag,
                            Placement = node.Placement,
                            Points = node.TotalPoints
                        });
                    }
                    Log($"Fetched standings from {graphQLResponse.Data.League.Standings.Nodes.Count} players.");
                }
                else
                {
                    Log("No standings found for this league.");
                    MessageBox.Show("No standings found for this league.");
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
                        standings(query: {perPage: 100, page: 1}) {
                            nodes {
                                entrant {
                                    id
                                    name
                                }
                                placement
                                totalPoints
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
                                    Placement = node.Placement,
                                    Points = node.TotalPoints
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
                                    totalPoints
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
                            Placement = node.Placement,
                            Points = node.TotalPoints
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
                if (lines.Length >= 7)
                {
                    ApiKeyTextBox.Text = lines[0];
                    UrlTextBox.Text = lines[1];
                    NetlifySiteIdTextBox.Text = lines[2];
                    NetlifyAccessTokenBox.Password = lines[3];

                    if (lines[4] == "Tournament")
                    {
                        LinkTypeComboBox.SelectedIndex = 0;
                    }
                    else if (lines[4] == "League")
                    {
                        LinkTypeComboBox.SelectedIndex = 1;
                    }

                    if (ColorConverter.ConvertFromString(lines[5]) is Color color)
                    {
                        ColorPicker.SelectedColor = color;
                    }

                    if (File.Exists(lines[6]))
                    {
                        droppedImagePath = lines[6];
                        DroppedImage.Source = new BitmapImage(new Uri(droppedImagePath));
                    }
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
                NetlifyAccessTokenBox.Password,
                (LinkTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(),
                ColorPicker.SelectedColor.HasValue ? ColorPicker.SelectedColor.Value.ToString() : string.Empty,
                droppedImagePath ?? string.Empty
            };
            File.WriteAllLines("settings.txt", lines);
            Log("Settings saved.");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (isServerModePrimed && isServerModeActive)
            {
                return;
            }
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

        private void PushToServerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            NetlifySiteIdTextBox.IsEnabled = true;
            NetlifyAccessTokenBox.IsEnabled = true;
        }

        private void PushToServerCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            NetlifySiteIdTextBox.IsEnabled = false;
            NetlifyAccessTokenBox.IsEnabled = false;
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

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string updateUrl = "https://github.com/CadeXR/StartGGgraphicGenerator/releases/download/untagged-42c06126b4552113e2f0/StartGGgraphicGenerator.zip";

                string tempZipPath = Path.Combine(Path.GetTempPath(), "StartGGgraphicGeneratorUpdate.zip");
                string extractPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UpdateTemp");

                using (var client = new HttpClient())
                {
                    Log("Downloading update...");
                    var response = await client.GetAsync(updateUrl);
                    response.EnsureSuccessStatusCode();

                    using (var fs = new FileStream(tempZipPath, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                Log("Extracting update...");
                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }
                ZipFile.ExtractToDirectory(tempZipPath, extractPath);

                Log("Replacing old files...");
                foreach (var file in Directory.GetFiles(extractPath))
                {
                    var fileName = Path.GetFileName(file);
                    if (fileName != "settings.txt")
                    {
                        var destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                        File.Copy(file, destinationPath, true);
                    }
                }

                Log("Update completed. Please restart the application.");
                MessageBox.Show("Update completed. Please restart the application.", "Update", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log($"Update failed: {ex.Message}");
                MessageBox.Show($"Update failed: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class Player
    {
        public string Name { get; set; }
        public int Placement { get; set; }
        public float? Points { get; set; }
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

    public class Tournament
    {
        public List<Event> Events { get; set; }
    }

    public class League
    {
        public EventConnection Events { get; set; }
        public Standings Standings { get; set; }
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
        public float? TotalPoints { get; set; }
        public PlayerDetails Player { get; set; }
    }

    public class Entrant
    {
        public string Name { get; set; }
    }

    public class PlayerDetails
    {
        public string Prefix { get; set; }
        public string GamerTag { get; set; }
    }

    public class Event
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Standings Standings { get; set; } // Added Standings property
    }
}
