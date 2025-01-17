namespace FFmpeg.UI
{
    public class GraphicsDrawable : IDrawable
    {
        public PointF StartPoint { get; set; }
        public List<PointF> DragPoints { get; set; } = [];

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Colors.DarkBlue;
            canvas.StrokeSize = 2;

            if (DragPoints?.Count > 0)
            {
                canvas.DrawRectangle(StartPoint.X, StartPoint.Y, 
                    DragPoints.Last().X - StartPoint.X, 
                    DragPoints.Last().Y - StartPoint.Y);
            }
        }
    }
}
