using FFmpeg.UI.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FFmpeg.UI
{
    public partial class SettingsViewModel : ObservableObject
    {
        private const string VersionPattern = "{0} v.{1}";

        public SettingsViewModel()
        {
            Version = string.Format(VersionPattern, AppInfo.Current.Name, AppInfo.Current.VersionString);
            resultsFolder = Preferences.Default.Get(Constants.CustomResultsFolderKey, string.Empty);
            ffmpegLocation = Preferences.Default.Get(Constants.FFMpegLocationKey, string.Empty);
            OnPropertyChanged(nameof(Version));
            OnPropertyChanged(nameof(ResultsFolder));
            OnPropertyChanged(nameof(FfmpegLocation));
        }

        [ObservableProperty]
        private string? version;

        [ObservableProperty]
        private string? resultsFolder;

        [ObservableProperty]
        private string? ffmpegLocation;

        [RelayCommand]
        public async Task Tap(string url)
        {
            await Launcher.OpenAsync(url);
        }

        [RelayCommand]
        public async Task SelectCustomResultsFolder()
        {
            var folder = await FolderPicker.Default.PickAsync();
            if (folder.IsSuccessful)
            {
                Preferences.Default.Set(Constants.CustomResultsFolderKey, folder.Folder.Path);
                resultsFolder = folder.Folder.Path;
                OnPropertyChanged(nameof(ResultsFolder));
            }
        }

        [RelayCommand]
        public async Task SelectFFMpegLocation()
        {
            var folder = await FolderPicker.Default.PickAsync();
            if (folder.IsSuccessful)
            {
                Preferences.Default.Set(Constants.FFMpegLocationKey, folder.Folder.Path);
                ffmpegLocation = folder.Folder.Path;
                OnPropertyChanged(nameof(FfmpegLocation));
            }
        }
    }
}
