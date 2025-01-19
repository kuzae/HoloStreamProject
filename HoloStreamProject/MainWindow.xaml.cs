using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Net.Http;
using System.Text;
using System.Windows.Controls;
using System.Xml.Linq;


namespace HoloStreamProject
{
    public partial class MainWindow : Window
    {

        private readonly Dictionary<int, string?> _currentStreamIds = new();
        private DispatcherTimer _refreshTimer;
        private static readonly string[] ChannelIds = {
            "UChAnqc_AY5_I3Px5dig3X1Q", // Inugami Korone
            "UC1DCedRgGHBdm81E1llLhOQ", // Usada Pekora
            "UCCzUftO8KOVkV4wQG1vkUvg", // Houshou Marine
            "UCjF7rKj4B7KQ_-Fcr_4g0fQ"  // FUWAMOCO
        };
        public MainWindow()
        {
            System.Diagnostics.Debug.WriteLine("Starting Holo Stream Project...");

            InitializeComponent();
            InitializeStreamState();
            Task.Run(() => YouTubeNotificationService.StartNotificationListenerAsync(OnNotificationReceived));
            Task.Run(() => SubscribeToNotifications());
            CheckAndLoadStreams();
        }
        private void InitializeStreamState()
        {
            for (int i = 0; i < 4; i++)
            {
                _currentStreamIds[i] = null; // Initially, no streams are live
            }

        }
        private async Task SubscribeToNotifications()
        {
            string callbackUrl = "https://ffc2-104-50-68-191.ngrok-free.app"; // Replace with your public URL
            foreach (var channelId in ChannelIds)
            {
                await SubscribeToChannelNotificationsAsync(channelId, callbackUrl);
            }
        }
        private async Task SubscribeToChannelNotificationsAsync(string channelId, string callbackUrl)
        {
            string hubUrl = "https://pubsubhubbub.appspot.com/subscribe";
            using var client = new HttpClient();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("hub.callback", callbackUrl), // Your callback URL
                new KeyValuePair<string, string>("hub.topic", $"https://www.youtube.com/xml/feeds/videos.xml?channel_id={channelId}"), // Channel's feed
                new KeyValuePair<string, string>("hub.mode", "subscribe"),
                new KeyValuePair<string, string>("hub.verify", "async")
            });

            HttpResponseMessage response = await client.PostAsync(hubUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to subscribe to notifications for channel {channelId}. HTTP {response.StatusCode}");
            }
        }
        private async Task OnNotificationReceived(string notificationPayload)
        {
            try
            {
                // Parse the XML notification
                var doc = XDocument.Parse(notificationPayload);

                // Extract video ID and channel ID
                var videoId = doc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "id")?.Value.Replace("yt:video:", "");
                var channelId = doc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "channelId")?.Value;

                if (string.IsNullOrEmpty(videoId) || string.IsNullOrEmpty(channelId))
                {
                    Console.WriteLine("Invalid notification payload: missing video or channel ID.");
                    return;
                }

                Console.WriteLine($"Notification received for channel {channelId}, video ID {videoId}");

                int streamIndex = Array.IndexOf(ChannelIds, channelId);

                if (streamIndex >= 0)
                {
                    // Update the stream UI directly
                    string url = $"https://cdpn.io/pen/debug/oNPzxKo?v={videoId}&autoplay=0&mute=1";
                    UpdateStreamUI(streamIndex, url, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing notification: {ex.Message}");
            }
        }
        private async Task CheckAndLoadStreams()
        {
            string apiKey = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("YouTube API key is missing. Please set it in the environment variables.", "Configuration Error");
                return;
            }

            try
            {
                // Initialize YouTube API service
                var youtubeService = new YouTubeService(new BaseClientService.Initializer
                {
                    ApiKey = apiKey
                });

                var liveStreams = new Dictionary<string, string?>();

                foreach (var channelId in ChannelIds)
                {
                    // Create the search request for this channel
                    var searchRequest = youtubeService.Search.List("snippet");
                    searchRequest.ChannelId = channelId;
                    searchRequest.EventType = SearchResource.ListRequest.EventTypeEnum.Live; // Only live streams
                    searchRequest.Type = "video";
                    searchRequest.MaxResults = 1; // Only one result per channel

                    // Execute the request and get the response
                    var searchResponse = await searchRequest.ExecuteAsync();

                    // Get the live video ID if available
                    var liveVideoId = searchResponse.Items.FirstOrDefault()?.Id.VideoId;
                    liveStreams[channelId] = liveVideoId;
                }

                // Update UI for each channel
                for (int i = 0; i < ChannelIds.Length; i++)
                {
                    string channelId = ChannelIds[i];
                    string? videoId = liveStreams.ContainsKey(channelId) ? liveStreams[channelId] : null;

                    if (_currentStreamIds.TryGetValue(i, out string? currentVideoId) && currentVideoId == videoId)
                    {
                        Console.WriteLine($"Stream {i + 1} is still live with the same video ID. Skipping refresh.");
                        continue;
                    }

                    _currentStreamIds[i] = videoId;
                    string url = !string.IsNullOrEmpty(videoId)
                        ? $"https://cdpn.io/pen/debug/oNPzxKo?v={videoId}&autoplay=0&mute=1"
                        : "about:blank";

                    bool hasLiveStream = !string.IsNullOrEmpty(videoId);
                    UpdateStreamUI(i, url, hasLiveStream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading streams: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateStreamUI(int streamIndex, string url, bool hasLiveStream)
        {
            // Determine which UI elements to update based on streamIndex
            switch (streamIndex)
            {
                case 0:
                    System.Diagnostics.Debug.WriteLine("Korone");
                    if (hasLiveStream)
                    {
                        Stream1.Source = new Uri(url);
                        Stream1Background.Visibility = Visibility.Hidden;
                        Stream1.Visibility = Visibility.Visible;
                    }
                    Stream1.Visibility = Visibility.Hidden;
                    Stream1Background.Visibility = Visibility.Visible;
                    Stream1Status.Visibility = Visibility.Visible;
                    Stream1Status.Text = "OFFLINE";
                    break;
                case 1:
                    System.Diagnostics.Debug.WriteLine("Pekora");
                    if (hasLiveStream)
                    {
                        Stream2.Source = new Uri(url);
                        Stream2Background.Visibility = Visibility.Hidden;
                        Stream2.Visibility = Visibility.Visible;
                    }
                    Stream2.Visibility = Visibility.Hidden;
                    Stream2Background.Visibility = Visibility.Visible;
                    Stream2Status.Visibility = Visibility.Visible;
                    Stream2Status.Text = "OFFLINE";
                    break;
                case 2:
                    System.Diagnostics.Debug.WriteLine("Senchou");
                    if (hasLiveStream)
                    {
                        Stream3.Source = new Uri(url);
                        Stream3Background.Visibility = Visibility.Hidden;
                        Stream3.Visibility = Visibility.Visible;
                    }
                    Stream3.Visibility = Visibility.Hidden;
                    Stream3Background.Visibility = Visibility.Visible;
                    Stream3Status.Visibility = Visibility.Visible;
                    Stream3Status.Text = "OFFLINE";
                    break;
                case 3:
                    System.Diagnostics.Debug.WriteLine("Fuwamoco");
                    if (hasLiveStream)
                    {
                        Stream4.Source = new Uri(url);
                        Stream4Background.Visibility = Visibility.Hidden;
                        Stream4.Visibility = Visibility.Visible;
                    }
                    Stream4.Visibility = Visibility.Hidden;
                    Stream4Background.Visibility = Visibility.Visible;
                    Stream4Status.Visibility = Visibility.Visible;
                    Stream4Status.Text = "OFFLINE";
                    break;
            }
        }
        // Helper method to set the WebView2 source
        private void SetStreamSource(int streamIndex, string url)
        {
            switch (streamIndex)
            {
                case 0: Stream1.Source = new Uri(url); break;
                case 1: Stream2.Source = new Uri(url); break;
                case 2: Stream3.Source = new Uri(url); break;
                case 3: Stream4.Source = new Uri(url); break;
            }
        }
    }
}
