using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FFmpeg.UI.Helpers;
using FFmpeg.UI.Models;

namespace FFmpeg.UI
{
    public partial class CutViewModel : ObservableObject
    {
        private const string GetVideoWidthCommand = "-i {0} 2>&1 | grep Video: | grep -Po '\\d{{3,5}}x\\d{{3,5}}' | cut -d'x' -f1";
        private const string GetVideoHeightCommand = "-i {0} 2>&1 | grep Video: | grep -Po '\\d{{3,5}}x\\d{{3,5}}' | cut -d'x' -f2";
        private const string GetVideoParamsFFProbe = "-v error -select_streams v:0 -show_entries stream=width,height -of default=nw=1:nk=1 \"{0}\"";
        private const string CropVideoCommand = "-i \"{0}\" -filter:v \"crop={1}:{2}:{3}:{4}\" \"{5}\"";

        public CutViewModel()
        {
            WeakReferenceMessenger.Default.Register<SelectedRegionMessage>(this, OnCoordinatesRecieved);
        }

        [ObservableProperty]
        private MediaSource? previewSource;

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private bool isUILocked;

        private FileResult selectedFile;
        private PointF videoResolution;
        private float out_w;
        private float out_h;
        private float x_res;
        private float y_res;
        private float scale_factor = 1;

        private int ffprobeCounter = 0;

        [RelayCommand]
        public async Task SelectFileToCutting()
        {
            selectedFile = await SelectVideoAsync();

            if (selectedFile != null)
            {
                previewSource = MediaSource.FromUri(selectedFile.FullPath);
                OnPropertyChanged(nameof(PreviewSource));

                string args = string.Format(GetVideoParamsFFProbe, selectedFile.FullPath);
                string workDir = Path.GetDirectoryName(selectedFile.FullPath);

                FFMpegHelper.Instance.FFMpegOutput += OnFFMpegOutput;
                await FFMpegHelper.Instance.RunFFProbeWindowsAsync(args, workDir);
            }
        }

        [RelayCommand]
        public void Play()
        {
            isPlaying = true;
            OnPropertyChanged(nameof(IsPlaying));
            WeakReferenceMessenger.Default.Send(new PlayStateMessage(isPlaying));
        }

        [RelayCommand]
        public void Pause()
        {
            isPlaying = false;
            OnPropertyChanged(nameof(IsPlaying));
            WeakReferenceMessenger.Default.Send(new PlayStateMessage(isPlaying));
        }

        [RelayCommand]
        public async Task Crop()
        {
            isUILocked = true;
            OnPropertyChanged(nameof(IsUILocked));
            WeakReferenceMessenger.Default.Send(new PlayStateMessage(false));
            string workDir = Path.GetDirectoryName(selectedFile.FullPath);
            string fileName = Path.GetFileNameWithoutExtension(selectedFile.FullPath);
            string extension = Path.GetExtension(selectedFile.FullPath);
            string outName = Path.Combine(workDir, fileName + "_cropped" + extension);
            string args = string.Format(CropVideoCommand, selectedFile.FullPath, out_w, out_h, x_res, y_res, outName);
            FFMpegHelper.Instance.FFMpegOutput += OnFFMpegOutput;
            await FFMpegHelper.Instance.RunFFmpegAsync(args, workDir);
            isUILocked = false;
            OnPropertyChanged(nameof(IsUILocked));
        }

        private void OnFFMpegOutput(object? _, string msg)
        {
            if (ffprobeCounter == 0)
            {
                videoResolution.X = float.Parse(msg);
                ffprobeCounter++;
            }
            else if (ffprobeCounter == 1)
            {
                videoResolution.Y = float.Parse(msg);
                FFMpegHelper.Instance.FFMpegOutput -= OnFFMpegOutput;
                ffprobeCounter = 0;
            }
            Console.WriteLine(msg);
        }

        private void OnCoordinatesRecieved(object recipient, SelectedRegionMessage msg)
        {
            //TODO: fix - calculate coordinates for smaller video
            scale_factor = 1;
            out_w = msg.FinishCoordinates.X - msg.StartCoordinates.X;
            out_h = msg.FinishCoordinates.Y - msg.StartCoordinates.Y;
            x_res = 0;
            y_res = 0;
            float realPlayerX = msg.UIPlayerSize.X;
            float realPlayerY = msg.UIPlayerSize.Y;
            float realStartX = msg.StartCoordinates.X;
            float realStartY = msg.StartCoordinates.Y;

            if (videoResolution.Y > msg.UIPlayerSize.Y || videoResolution.X > msg.UIPlayerSize.X)
            {
                scale_factor = (videoResolution.Y / msg.UIPlayerSize.Y) - 1;
            }

            if (scale_factor != 1)
            {
                realPlayerX = realPlayerX * scale_factor + realPlayerX;
                realPlayerY = realPlayerY * scale_factor + realPlayerY;
                realStartX = realStartX * scale_factor + realStartX;
                realStartY = realStartY * scale_factor + realStartY;
            }

            x_res = msg.StartCoordinates.X - ((msg.UIPlayerSize.X - videoResolution.X) / 2);
            y_res = msg.StartCoordinates.Y - ((msg.UIPlayerSize.Y - videoResolution.Y) / 2);
        }

        //TODO: move pickers to helper
        private async Task<FileResult> SelectVideoAsync()
        {
            FileResult results = null;
            var videoFileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "video/*" } }, // MIME type for Android
                { DevicePlatform.WinUI, new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv" } }, //TODO: add many file types 
            });

            var pickOptions = new PickOptions
            {
                PickerTitle = "Select a Video File",
                FileTypes = videoFileTypes
            };

            results = await FilePicker.Default.PickAsync(pickOptions);
            return results;
        }
    }
}
