using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Net.Http;
using System.Text;
using System.Windows.Controls;
using System.Xml.Linq;
using HolostreamApp.Services;


namespace HoloStreamProject
{
    public partial class MainWindow : Window
    {

        private List<Channel> _channels;
        private readonly Dictionary<int, string?> _currentStreamIds = new();
        private DispatcherTimer _refreshTimer;
        private readonly ScheduleScraper _scheduleScraper = new ScheduleScraper();

        public MainWindow()
        {
            System.Diagnostics.Debug.WriteLine("Starting Holo Stream Project...");
            InitializeComponent();
            LoadChannels();
            InitializeStreamState();
            Task.Run(() => YouTubeNotificationService.StartNotificationListenerAsync(OnNotificationReceived));
            Task.Run(() => SubscribeToNotifications());
            CheckAndLoadStreamsFromSchedule();
        }
        private void LoadChannels()
        {
            try
            {
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "channels.json");
                string json = File.ReadAllText(jsonPath);
                var channelData = JsonSerializer.Deserialize<ChannelData>(json);

                if (channelData?.Channels == null)
                {
                    throw new Exception("Failed to load channel data.");
                }

                _channels = channelData.Channels;

                System.Diagnostics.Debug.WriteLine("Channels loaded:");
                foreach (var channel in _channels)
                {
                    System.Diagnostics.Debug.WriteLine($"Key: {channel.Key}, Name: {channel.Name}, ID: {channel.Id}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading channels: {ex.Message}");
            }
        }
        private void InitializeStreamState()
        {
            for (int i = 0; i < 4; i++)
            {
                _currentStreamIds[i] = null;
            }
        }
        private void UpdateStreamUI(int streamIndex, string url, bool hasLiveStream)
        {
            switch (streamIndex)
            {
                case 0: // Stream 1: Korone
                    System.Diagnostics.Debug.WriteLine("Korone");
                    if (hasLiveStream)
                    {
                        Stream1.Source = new Uri(url);
                        Stream1.Visibility = Visibility.Visible;
                        Stream1Background.Visibility = Visibility.Hidden;
                        Stream1Status.Visibility = Visibility.Hidden;
                        Stream1ReloadButton.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        Stream1.Visibility = Visibility.Hidden;
                        Stream1Background.Visibility = Visibility.Visible;
                        Stream1Status.Visibility = Visibility.Visible;
                        Stream1Status.Text = "OFFLINE";
                        Stream1ReloadButton.Visibility = Visibility.Visible;
                    }
                    break;

                case 1: // Stream 2: Pekora
                    System.Diagnostics.Debug.WriteLine("Pekora");
                    if (hasLiveStream)
                    {
                        Stream2.Source = new Uri(url);
                        Stream2.Visibility = Visibility.Visible;
                        Stream2Background.Visibility = Visibility.Hidden;
                        Stream2Status.Visibility = Visibility.Hidden;
                        Stream2ReloadButton.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        Stream2.Visibility = Visibility.Hidden;
                        Stream2Background.Visibility = Visibility.Visible;
                        Stream2Status.Visibility = Visibility.Visible;
                        Stream2Status.Text = "OFFLINE";
                        Stream2ReloadButton.Visibility = Visibility.Visible;
                    }
                    break;

                case 2: // Stream 3: Marine
                    System.Diagnostics.Debug.WriteLine("Senchou");
                    if (hasLiveStream)
                    {
                        Stream3.Source = new Uri(url);
                        Stream3.Visibility = Visibility.Visible;
                        Stream3Background.Visibility = Visibility.Hidden;
                        Stream3Status.Visibility = Visibility.Hidden;
                        Stream3ReloadButton.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        Stream3.Visibility = Visibility.Hidden;
                        Stream3Background.Visibility = Visibility.Visible;
                        Stream3Status.Visibility = Visibility.Visible;
                        Stream3Status.Text = "OFFLINE";
                        Stream3ReloadButton.Visibility = Visibility.Visible;
                    }
                    break;

                case 3: // Stream 4: Fuwamoco
                    System.Diagnostics.Debug.WriteLine("Fuwamoco");
                    if (hasLiveStream)
                    {
                        Stream4.Source = new Uri(url);
                        Stream4.Visibility = Visibility.Visible;
                        Stream4Background.Visibility = Visibility.Hidden;
                        Stream4Status.Visibility = Visibility.Hidden;
                        Stream4ReloadButton.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        Stream4.Visibility = Visibility.Hidden;
                        Stream4Background.Visibility = Visibility.Visible;
                        Stream4Status.Visibility = Visibility.Visible;
                        Stream4Status.Text = "OFFLINE";
                        Stream4ReloadButton.Visibility = Visibility.Visible;
                    }
                    break;

                default:
                    throw new ArgumentException($"Invalid stream index: {streamIndex}");
            }
        }
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
        private string ExtractVideoId(string url)
        {
            var match = System.Text.RegularExpressions.Regex.Match(url, @"v=([^&]+)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
        private async void Stream1ReloadButton_Click(object sender, RoutedEventArgs e) => await ReloadStreamAsync(0);
        private async void Stream2ReloadButton_Click(object sender, RoutedEventArgs e) => await ReloadStreamAsync(1);
        private async void Stream3ReloadButton_Click(object sender, RoutedEventArgs e) => await ReloadStreamAsync(2);
        private async void Stream4ReloadButton_Click(object sender, RoutedEventArgs e) => await ReloadStreamAsync(3);
        private async Task SubscribeToNotifications()
        {
            string callbackUrl = "https://ffc2-104-50-68-191.ngrok-free.app";
            foreach (var channel in _channels)
            {
                await SubscribeToChannelNotificationsAsync(channel.Id, callbackUrl);
            }
        }
        private async Task SubscribeToChannelNotificationsAsync(string channelId, string callbackUrl)
        {
            string hubUrl = "https://pubsubhubbub.appspot.com/subscribe";
            using var client = new HttpClient();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("hub.callback", callbackUrl),
                new KeyValuePair<string, string>("hub.topic", $"https://www.youtube.com/xml/feeds/videos.xml?channel_id={channelId}"),
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
                var doc = XDocument.Parse(notificationPayload);

                var videoId = doc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "id")?.Value.Replace("yt:video:", "");
                var channelId = doc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "channelId")?.Value;

                if (string.IsNullOrEmpty(videoId) || string.IsNullOrEmpty(channelId))
                {
                    Console.WriteLine("Invalid notification payload: missing video or channel ID.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Notification received for channel {channelId}, video ID {videoId}");

                // Find the matching channel and its index
                var matchingChannel = _channels.FirstOrDefault(channel => channel.Id == channelId);

                if (matchingChannel != null && int.TryParse(matchingChannel.Key, out int streamIndex))
                {
                    string url = $"https://cdpn.io/pen/debug/oNPzxKo?v={videoId}&autoplay=0&mute=1";
                    UpdateStreamUI(streamIndex, url, true);
                }
                else
                {
                    Console.WriteLine($"Channel ID {channelId} not found in the loaded channels.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing notification: {ex.Message}");
            }
        }

        private async Task ReloadStreamAsync(int streamIndex)
        {
            string scheduleUrl = "https://hololive.hololivepro.com/en/schedule/";

            try
            {
                // Fetch live streams using the schedule scraper
                var scheduleItems = await _scheduleScraper.ScrapeStreamsAsync(scheduleUrl);

                if (scheduleItems == null || !scheduleItems.Any())
                {
                    MessageBox.Show("No live streams found in the schedule.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Get currently displayed streams to exclude
                var displayedStreams = _currentStreamIds.Values.Where(id => !string.IsNullOrEmpty(id)).ToHashSet();

                // Filter streams that match the channel names and are not currently displayed
                var availableStreams = scheduleItems
                    .Where(item => item.LiveStatus == "Live" && !displayedStreams.Contains(ExtractVideoId(item.Link)))
                    .ToList();

                if (!availableStreams.Any())
                {
                    MessageBox.Show("No new streams are available to display.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Select a random available stream
                var random = new Random();
                var randomStream = availableStreams[random.Next(availableStreams.Count)];

                // Extract video ID and prepare the URL
                string videoId = ExtractVideoId(randomStream.Link);
                string url = !string.IsNullOrEmpty(videoId)
                    ? $"https://cdpn.io/pen/debug/oNPzxKo?v={videoId}&autoplay=0&mute=1"
                    : "about:blank";

                // Update the UI with the selected stream
                _currentStreamIds[streamIndex] = videoId;
                SetStreamSource(streamIndex, url);
                UpdateStreamUI(streamIndex, url, hasLiveStream: true);

                Console.WriteLine($"Stream {streamIndex + 1} updated to random live video: {videoId}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reloading stream: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task CheckAndLoadStreamsFromSchedule()
        {
            string scheduleUrl = "https://hololive.hololivepro.com/en/schedule/";

            try
            {
                // Scrape the schedule for live streams
                var scheduleItems = await _scheduleScraper.ScrapeStreamsAsync(scheduleUrl);

                // Log all scraped schedule items for debugging
                foreach (var item in scheduleItems)
                {
                    System.Diagnostics.Debug.WriteLine($"Start: {item.Start}, Name: {item.Name}, Text: {item.Text}, Link: {item.Link}, Live Status: {item.LiveStatus}");
                }

                // Iterate through the channels list
                foreach (var channel in _channels)
                {
                    if (int.TryParse(channel.Key, out int streamIndex))
                    {
                        // Find a matching item from the schedule by channel name
                        var matchingItem = scheduleItems.FirstOrDefault(item => item.Name.Contains(channel.Name));

                        if (matchingItem != null && matchingItem.LiveStatus == "Live")
                        {
                            // Extract video ID and construct the video URL
                            string videoId = ExtractVideoId(matchingItem.Link);
                            string url = !string.IsNullOrEmpty(videoId)
                                ? $"https://cdpn.io/pen/debug/oNPzxKo?v={videoId}&autoplay=0&mute=1"
                                : "about:blank";

                            // Update only if the video ID has changed
                            if (_currentStreamIds.TryGetValue(streamIndex, out string? currentVideoId) && currentVideoId == videoId)
                            {
                                System.Diagnostics.Debug.WriteLine($"Stream {streamIndex} is still live with the same video ID. Skipping refresh.");
                                continue;
                            }

                            // Update the current stream and UI
                            _currentStreamIds[streamIndex] = videoId;
                            UpdateStreamUI(streamIndex, url, true);
                        }
                        else
                        {
                            // No matching live stream found, clear the UI for this channel
                            _currentStreamIds[streamIndex] = null;
                            UpdateStreamUI(streamIndex, "about:blank", false);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid stream key for channel: {channel.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading streams: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }























        /***
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
                var youtubeService = new YouTubeService(new BaseClientService.Initializer
                {
                    ApiKey = apiKey
                });

                var liveStreams = new Dictionary<string, string?>();

                foreach (var channelId in ChannelIds)
                {
                    var searchRequest = youtubeService.Search.List("snippet");
                    searchRequest.ChannelId = channelId;
                    searchRequest.EventType = SearchResource.ListRequest.EventTypeEnum.Live;
                    searchRequest.Type = "video";
                    searchRequest.MaxResults = 1;

                    var searchResponse = await searchRequest.ExecuteAsync();
                    var liveVideoId = searchResponse.Items.FirstOrDefault()?.Id.VideoId;
                    liveStreams[channelId] = liveVideoId;
                }

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
        ***/
    }
}
