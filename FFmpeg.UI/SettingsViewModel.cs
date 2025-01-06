using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FFmpeg.UI
{
    public partial class SettingsViewModel : ObservableObject
    {
        private const string VersionPattern = "{0} v.{1}";

        public SettingsViewModel()
        {
            Version = string.Format(VersionPattern, AppInfo.Current.Name, AppInfo.Current.VersionString);
            OnPropertyChanged(nameof(Version));
        }

        [ObservableProperty]
        private string? version;

        [RelayCommand]
        public async Task Tap(string url)
        {
            await Launcher.OpenAsync(url);
        } 
    }
}
