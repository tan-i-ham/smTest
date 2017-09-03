using System.Collections.Generic;

namespace smTest
{
    public class FBChannelModel
    {
        public Sender sender { get; set; }
        public Recipient recipient { get; set; }
        public long timestamp { get; set; }
        public FBMessage message { get; set; }
        public Postback postback { get; set; }
        public Location location { get; set; }
    }

    public class Location
    {
        public double altitude { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string name { get; set; }
    }

    public class Postback
    {
        public string payload { get; set; }
    }

    public class Sender
    {
        public string id { get; set; }
    }

    public class Recipient
    {
        public string id { get; set; }
    }

    public class FBMessage
    {
        public string mid { get; set; }
        public int seq { get; set; }
        public string text { get; set; }
        public List<FBAttachment> attachments { get; set; }
        public QuickReplyMessage quick_reply { get; set; }
        public string sticker_id { get; set; }
    }

    public class QuickReplyMessage
    {
        public string payload { get; set; }
    }

    public class FBAttachment
    {
        public string title { get; set; }
        public string url { get; set; }
        public string type { get; set; }
        public Payload payload { get; set; }
    }

    public class Payload
    {
        public string url { get; set; }
    }
    /// <summary>
    /// ///////////////////
    /// </summary>
    public class Messenger
    {
        public MessengerChannelData ChannelData { get; set; }
    }
    public class MessengerChannelData
    {
        public string notification_type { get; set; }
        public MessengerAttachment attachment { get; set; }
    }
    public class MessengerAttachment
    {
        public string type { get; set; }
        public MessengerPayload payload { get; set; }
    }

    public class MessengerPayload
    {
        public string template_type { get; set; }
        public MessengerElement[] elements { get; set; }
    }

    public class MessengerElement
    {
        public string title { get; set; }
        public string subtitle { get; set; }
        public string item_url { get; set; }
        public string image_url { get; set; }
        public MessengerButton[] buttons { get; set; }
    }
    public class MessengerButton
    {
        public string type { get; set; }
        public string url { get; set; }
        public string title { get; set; }
        public string payload { get; set; }
    }
}