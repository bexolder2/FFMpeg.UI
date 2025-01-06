using CommunityToolkit.Mvvm.ComponentModel;

namespace FFmpeg.UI.Models
{
    public partial class UIFile : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string path;

        public UIFile(string name, string path)
        {
            this.name = name;
            this.path = path;
        }
    }
}
