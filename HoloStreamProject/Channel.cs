using System.Collections.Generic;

namespace HoloStreamProject
{
    public class Channel
    {
        public string Id { get; set; }    // YouTube Channel ID
        public string Key { get; set; }   // Window key (e.g., "1", "2", etc.)
        public string Name { get; set; }  // Channel Name
        public string Url { get; set; }   // YouTube Channel URL
    }

    public class ChannelData
    {
        public List<Channel> Channels { get; set; }  // List of all channels
    }
}
