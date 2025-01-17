namespace FFmpeg.UI.Models
{
    public class PlayStateMessage
    {
        public bool IsPlayed { get; set; }

        public PlayStateMessage(bool isPlayed)
        {
            IsPlayed = isPlayed;
        }
    }
}
