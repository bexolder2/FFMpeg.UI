namespace FFmpeg.UI
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            var platform = DeviceInfo.Current.Platform;

            if (platform == DevicePlatform.WinUI)
            {
                if (sender is Editor editor)
                {
                    editor.CursorPosition = editor.Text.Length - 1;
                    editor.Focus();
                }
            }
        }
    }
}
