namespace FFmpeg.UI
{
    public partial class App : Application
    {
        private DevicePlatform platform = DeviceInfo.Current.Platform;

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Window wnd = null;
            if (platform == DevicePlatform.Android)
            {
                wnd = new Window(new AppShellAndroid());
            }
            else if (platform == DevicePlatform.WinUI)
            {
                wnd = new Window(new AppShell());
            }

            return wnd;
        }
    }
}