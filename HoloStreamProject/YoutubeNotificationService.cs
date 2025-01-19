using System.IO;
using System.Net;
using System.Text;

public static class YouTubeNotificationService
{
    public static async Task StartNotificationListenerAsync(Func<string, Task> handleNotification)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5000/"); // Replace with your callback URL
        listener.Start();
        System.Diagnostics.Debug.WriteLine("Listening for PubSubHubbub notifications...");

        while (true)
        {
            var context = await listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "GET" && request.QueryString["hub.challenge"] != null)
            {
                // Respond to the subscription challenge
                string challenge = request.QueryString["hub.challenge"];
                byte[] buffer = Encoding.UTF8.GetBytes(challenge);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            else if (request.HttpMethod == "POST")
            {
                // Handle notifications
                using var reader = new StreamReader(request.InputStream);
                string body = await reader.ReadToEndAsync();
                System.Diagnostics.Debug.WriteLine($"Notification received: {body}");

                // Extract the channel ID or video ID from the notification
                string channelId = ExtractChannelIdFromNotification(body); // Implement this parser

                if (!string.IsNullOrEmpty(channelId))
                {
                    // Trigger the API check for the channel
                    await handleNotification(channelId);
                }
            }

            response.StatusCode = 200;
            response.Close();
        }
    }

    private static string ExtractChannelIdFromNotification(string notificationXml)
    {
        // Parse the XML body to extract the channel ID or video ID
        // Example: Extract <yt:channelId>UChAnqc_AY5_I3Px5dig3X1Q</yt:channelId>
        if (notificationXml.Contains("<yt:channelId>"))
        {
            int start = notificationXml.IndexOf("<yt:channelId>") + "<yt:channelId>".Length;
            int end = notificationXml.IndexOf("</yt:channelId>", start);
            return notificationXml.Substring(start, end - start);
        }
        return null;
    }
}
