using CommunityToolkit.Mvvm.Messaging;
using FFmpeg.UI.Models;
using MauiIcons.Core;

namespace FFmpeg.UI;

public partial class CutPage : ContentPage
{
    private GraphicsDrawable graphicsDrawable = null;

    public CutPage()
	{
		InitializeComponent();
        _ = new MauiIcon();
        graphicsDrawable = new GraphicsDrawable();
        Graphics.Drawable = graphicsDrawable;

        WeakReferenceMessenger.Default.Register<PlayStateMessage>(this, OnPLayChanged);
    }

    private void OnPLayChanged(object _, PlayStateMessage message)
    {
        if (message.IsPlayed)
        {
            Progress.Maximum = Player.Duration.TotalSeconds;
            Player.Play();
        }
        else
        {
            Player.Pause();
        }
    }

    private void GraphicsView_StartInteraction(object sender, TouchEventArgs e)
    {
        graphicsDrawable.StartPoint = e.Touches[0];
        Graphics.Invalidate();
    }

    private void GraphicsView_DragInteraction(object sender, TouchEventArgs e)
    {
        graphicsDrawable.DragPoints.Add(e.Touches[0]);
        Graphics.Invalidate();
    }

    private void GraphicsView_EndInteraction(object sender, TouchEventArgs e)
    {
        var player = new PointF((float)Player.Width, (float)Player.Height);
        if (graphicsDrawable.DragPoints.Count > 0)
        {
            WeakReferenceMessenger.Default.Send(
                new SelectedRegionMessage(player,
                graphicsDrawable.StartPoint,
                graphicsDrawable.DragPoints.Last()));
        }
        graphicsDrawable.DragPoints.Clear();
    }

    private void Slider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        Player.SeekTo(TimeSpan.FromSeconds(e.NewValue));
    }
}