using FFmpeg.UI.Models;
using System.Diagnostics;

namespace FFmpeg.UI.Helpers
{
    public class FFMpegHelper
    {
        #region Singletone

        private static Lazy<FFMpegHelper> instance = new Lazy<FFMpegHelper>();
        public static FFMpegHelper Instance => instance.Value;

        #endregion

        private DevicePlatform platform = DeviceInfo.Current.Platform;

        public event EventHandler<string> FFMpegOutput;

        public async Task RunFFmpegAsync(string args, string workDir)
        {
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

        public async Task RunFFProbeWindowsAsync(string args, string workDir)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe.exe",
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
            try
            {
                Debug.WriteLine($"RunFFMpegAndroidAsync: {args}");
                Laerdal.FFmpeg.FFmpegConfig.IgnoreSignal(24);
                Laerdal.FFmpeg.FFmpeg.Execute(args);
            }
            catch (Exception ex) { }
        }

        private async Task ReadOutputAsync(StreamReader reader)
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                FFMpegOutput?.Invoke(this, line);
            }
        }
    }
}
