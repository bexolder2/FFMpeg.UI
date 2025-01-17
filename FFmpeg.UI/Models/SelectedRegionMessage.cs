namespace FFmpeg.UI.Models
{
    public class SelectedRegionMessage
    {
        public PointF UIPlayerSize { get; private set; }

        public PointF StartCoordinates { get; private set; }

        public PointF FinishCoordinates { get; private set; }

        public SelectedRegionMessage(PointF playerSize, PointF start, PointF finish)
        {
            UIPlayerSize = playerSize;
            StartCoordinates = start;
            FinishCoordinates = finish;
        }
    }
}
