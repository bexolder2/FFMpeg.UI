using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFmpeg.UI.Models;
using System.Diagnostics;

namespace FFmpeg.UI
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly int MaximumConcurrencyCount = Environment.ProcessorCount / 2;

        private const string GenerateStabDataCommand = "-i \"{0}\" -vf vidstabdetect -f null -"; 
        private const string StabVideoCommand = "-i \"{0}\" -vf vidstabtransform \"{1}\"";
        private const string MergeVideosVertically = "-i {0} -i {1} -filter_complex vstack=inputs=2 {2}";
        private const string MergeVideosHorizontally = "-i {0} -i {1} -filter_complex hstack=inputs=2 {2}";

        [ObservableProperty]
        private bool isNeedGenerateMerged;

        [ObservableProperty]
        private bool isUILocked;

        [ObservableProperty]
        private string? logData;

        [ObservableProperty]
        private List<UIFile>? selectedFiles;

        [ObservableProperty]
        private MediaSource? previewSource;

        [RelayCommand]
        public async Task SelectFilesToProcessing()
        {
            var files = await SelectVideosAsync();

            if (files?.Count() > 0)
            {
                if (selectedFiles == null)
                {
                    selectedFiles = new List<UIFile>();
                }

                foreach (var file in files)
                {
                    selectedFiles.Add(new UIFile(file.FileName, file.FullPath));
                }
                OnPropertyChanged(nameof(SelectedFiles));
            }
        }

        [RelayCommand]
        public void ClearSelectedFiles()
        {
            selectedFiles = null;
            logData = string.Empty;
            OnPropertyChanged(nameof(SelectedFiles));
            OnPropertyChanged(nameof(LogData));
        }

        [RelayCommand]
        public async Task RunStabilizationTask()
        {
            var platform = DeviceInfo.Current.Platform;
            UpdateUILockState(true);
            List<Func<Task>> tasks = [];

            if (platform == DevicePlatform.WinUI || platform == DevicePlatform.Android)
            {
                if (selectedFiles?.Count > 0)
                {
                    foreach (var file in selectedFiles.ToList())
                    {
                        tasks.Add(() => ExecuteStabilizationTaskAsync(file));
                    }
                }
            }

            await RunWithMaxConcurrency(tasks);
            UpdateUILockState(false);
        }

        [RelayCommand]
        private void PlayPreview(UIFile file)
        {
            if (file != null)
            {
                previewSource = MediaSource.FromUri(file.Path);
                OnPropertyChanged(nameof(PreviewSource));
            }
        }

        private async Task RunWithMaxConcurrency(IEnumerable<Func<Task>> tasks)
        {
            using var semaphore = new SemaphoreSlim(MaximumConcurrencyCount);

            var tasks_ = tasks.Select(async task =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await task();
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks_);
        }

        private async Task ExecuteStabilizationTaskAsync(UIFile file)
        {
            string videoFolderPath = Path.Combine(FileSystem.CacheDirectory, Path.GetFileNameWithoutExtension(file.Name));
            Debug.WriteLine($"Tmp folder path: {videoFolderPath}");
            string customPath = Preferences.Default.Get(Constants.CustomResultsFolderKey, string.Empty);
            try
            {
                Directory.CreateDirectory(videoFolderPath);
                await GenerateStabilizationDataAsync(file, videoFolderPath);
                string newFilePath = await StabilizeVideoAsync(file, videoFolderPath);
                if (!string.IsNullOrEmpty(newFilePath))
                {
                    Debug.WriteLine($"New file path: {newFilePath}");
                    string destinationPath = Path.Combine(Path.GetDirectoryName(file.Path), Path.GetFileName(newFilePath));
                    
                    if (!string.IsNullOrEmpty(customPath))
                    {
                        destinationPath = Path.Combine(customPath, Path.GetFileName(newFilePath));
                    }
                    File.Copy(newFilePath, destinationPath, true);
                }

                if (isNeedGenerateMerged)
                {
                    string sourcePath = await MergeVideosAsync(file.Name, videoFolderPath);
                    string destinationPath = Path.Combine(Path.GetDirectoryName(file.Path), Path.GetFileName(sourcePath));
                    if (!string.IsNullOrEmpty(customPath))
                    {
                        destinationPath = Path.Combine(customPath, Path.GetFileName(sourcePath));
                    }
                    //TODO: Add queue for previews 
                    File.Copy(sourcePath, destinationPath);

                    if (selectedFiles.Count == 1)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            previewSource = MediaSource.FromUri(destinationPath);
                            OnPropertyChanged(nameof(PreviewSource));
                        });
                    }
                }
            }
            catch (Exception ex) { }
            finally
            {
                ClearTmp(videoFolderPath);
            }
        }

        private async Task GenerateStabilizationDataAsync(UIFile file, string tmpPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(file.Name) && !string.IsNullOrEmpty(file.Path))
                {
                    string targetPath = Path.Combine(tmpPath, file.Name);
                    Debug.WriteLine($"GenerateStabilizationDataAsync targetPath: {targetPath}");
                    File.Copy(file.Path, targetPath, true);
                    string combinedArgs = string.Format(GenerateStabDataCommand, targetPath);
                    Debug.WriteLine($"GenerateStabilizationDataAsync combinedArgs: {combinedArgs}");
                    await RunFFmpegAsync(combinedArgs, tmpPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GenerateStabilizationDataAsync: {ex.Message}");
            }
        }

        private async Task<string> StabilizeVideoAsync(UIFile file, string tmpPath)
        {
            string outFileName = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(file.Name) && !string.IsNullOrEmpty(file.Path))
                {
                    outFileName = Path.Combine(tmpPath, Path.GetFileNameWithoutExtension(file.Name) + "_stabilized" + Path.GetExtension(file.Name));
                    string targetPath = Path.Combine(tmpPath, file.Name);
                    await RunFFmpegAsync(string.Format(StabVideoCommand, targetPath, outFileName), tmpPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StabilizeVideoAsync: {ex.Message}");
            }

            return outFileName;
        }

        private async Task<string> MergeVideosAsync(string fileName, string workDir)
        {
            string mergedPath = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    string name = Path.GetFileNameWithoutExtension(fileName);
                    string videoPath = Path.Combine(FileSystem.CacheDirectory, name, fileName);
                    string stabVideoPath = Path.Combine(FileSystem.CacheDirectory, name, name + "_stabilized" + Path.GetExtension(fileName));
                    mergedPath = Path.Combine(FileSystem.CacheDirectory, name, name + "_merged" + Path.GetExtension(fileName));
                    var mergeCommand = string.Format(MergeVideosHorizontally, videoPath, stabVideoPath, mergedPath);

                    Debug.WriteLine($"MergeVideosAsync mergedPath: {mergedPath}");
                    Debug.WriteLine($"MergeVideosAsync mergeCommand: {mergeCommand}");

                    await RunFFmpegAsync(mergeCommand, workDir);
                }
            }
            catch (Exception ex) 
            {
                Debug.WriteLine($"MergeVideosAsync: {ex.Message}");
            }

            return mergedPath;
        }

        private void ClearTmp(string tmpPath)
        {
            Debug.WriteLine($"ClearTmp path: {tmpPath}");
            try
            {
                Directory.Delete(tmpPath, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ClearTmp: {ex.Message}");
            }
        }

        private async Task RunFFmpegAsync(string args, string workDir)
        {
            var platform = DeviceInfo.Current.Platform;
            if (platform == DevicePlatform.WinUI)
            {
                await RunFFMpegWindowsAsync(args, workDir);
            }
            else if (platform == DevicePlatform.Android)
            {
                await RunFFMpegAndroidAsync(args, workDir);
            }
        }

        private async Task RunFFMpegWindowsAsync(string args, string workDir)
        {
            string ffmpegLocation = Preferences.Default.Get(Constants.FFMpegLocationKey, string.Empty);
            if (string.IsNullOrEmpty(ffmpegLocation))
            {
                ffmpegLocation = "ffmpeg.exe";
            }
            else
            {
                ffmpegLocation = Path.Combine(ffmpegLocation, "ffmpeg.exe");
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegLocation,
                    WorkingDirectory = workDir,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        // Read the output asynchronously
                        StreamReader outputReader = process.StandardOutput;
                        StreamReader errorReader = process.StandardError;

                        // Asynchronously read the output stream in real-time
                        Task outputTask = ReadOutputAsync(outputReader);
                        Task errorTask = ReadOutputAsync(errorReader);

                        await process.WaitForExitAsync();
                        await Task.WhenAll(outputTask, errorTask);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RunFFmpegAsync args: {args}");
                Debug.WriteLine($"RunFFmpegAsync: {ex.Message}");
            }
        }

        private async Task RunFFMpegAndroidAsync(string args, string workDir)
        {
#if ANDROID
            try
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var currentActivity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                    await FFMpeg.Xamarin.FFMpegLibrary.Run(currentActivity, args, (log) => {
                        LogData += log + "\n";
                        OnPropertyChanged(nameof(LogData));
                    });
                });
            }
            catch (Exception ex)
            {

            }
#endif
        }

        private async Task ReadOutputAsync(StreamReader reader)
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LogData += line + "\n";
                    OnPropertyChanged(nameof(LogData));
                });
            }
        }

        private async Task<IEnumerable<FileResult>> SelectVideosAsync()
        {
            IEnumerable<FileResult> results = null;
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

            results = await FilePicker.Default.PickMultipleAsync(pickOptions);
            return results;
        }

        private void UpdateUILockState(bool newState)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                isUILocked = newState;
                OnPropertyChanged(nameof(IsUILocked));
            });
        }
    }
}
