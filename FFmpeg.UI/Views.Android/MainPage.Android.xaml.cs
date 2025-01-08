namespace FFmpeg.UI;

public partial class MainPageAndroid : ContentPage
{
	public MainPageAndroid()
	{
		InitializeComponent();
	}

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        var platform = DeviceInfo.Current.Platform;

        if (platform == DevicePlatform.Android)
        {
            if (sender is Editor editor)
            {
                editor.CursorPosition = editor.Text.Length - 1;
                editor.Focus();
            }
        }
    }
}