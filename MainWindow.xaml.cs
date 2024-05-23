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
using System.Diagnostics;

namespace StartGGgraphicGenerator
{
    public partial class MainWindow : Window
    {
        private static string apiUrl = "https://api.start.gg/gql/alpha";
        private static string apiToken = "";
        private static List<Player> players = new List<Player>();
        private static List<Event> events = new List<Event>();
        private string selectedColor = "#0000FF";
        private string selectedFont = "Arial";

        public MainWindow()
        {
            InitializeComponent();
            LoadApiKey();
        }

        private async void FetchDataButton_Click(object sender, RoutedEventArgs e)
        {
            apiToken = ApiKeyTextBox.Text;
            string url = UrlTextBox.Text;
            string slug = ExtractSlugFromUrl(url);
            selectedColor = ColorTextBox.Text;

            if (!string.IsNullOrEmpty(slug))
            {
                Log("Fetching events from tournament...");
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
                    HTMLGenerator.SaveHtmlToFile(players, selectedFont, selectedColor);
                    Log("HTML file saved successfully.");
                }
                else
                {
                    Log("No players found for this event.");
                    MessageBox.Show("No players found for this event.");
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

        private void SaveHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HTMLGenerator.SaveHtmlToFile(players, selectedFont, selectedColor);
                Log("HTML file saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving the HTML file: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            Debug.WriteLine(message);
        }

        private void LoadApiKey()
        {
            if (File.Exists("apiKey.txt"))
            {
                apiToken = File.ReadAllText("apiKey.txt");
                ApiKeyTextBox.Text = apiToken;
            }
        }

        private void SaveApiKey()
        {
            File.WriteAllText("apiKey.txt", apiToken);
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && (textBox.Text == textBox.Tag.ToString()))
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
        public Tournament Tournament { get; set; }
        public Event Event { get; set; }
    }

    public class Tournament
    {
        public List<Event> Events { get; set; }
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
        public string Id { get; set; }
        public string Name { get; set; }
    }
}

