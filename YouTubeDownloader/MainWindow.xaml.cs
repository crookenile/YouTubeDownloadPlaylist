using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace YouTubeDownloader;

public partial class MainWindow : Window
{
    public ObservableCollection<DownloadItem> Downloads { get; set; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DownloadList.ItemsSource = Downloads;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = "Select Folder", // اسم افتراضي مش هيتحفظ
            Filter = "Folder|*.this.is.not.used"
        };

        if (dialog.ShowDialog() == true)
        {
            string folder = System.IO.Path.GetDirectoryName(dialog.FileName)!;
            FolderBox.Text = folder; // هنا بيتخزن المسار
        }
    }


    private async void Download_Click(object sender, RoutedEventArgs e)
    {
        string url = UrlBox.Text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            System.Windows.MessageBox.Show("Enter a playlist URL!");
            return;
        }
        if (string.IsNullOrEmpty(FolderBox.Text))
        {
            System.Windows.MessageBox.Show("Select a download folder!");
            return;
        }


        StatusText.Text = "Starting download...";
        await DownloadPlaylist(url, FolderBox.Text);
        StatusText.Text = "✅ Finished!";


    }

    private async Task DownloadPlaylist(string playlistUrl, string outputPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "yt-dlp.exe",
            Arguments = $"-f bestvideo+bestaudio --merge-output-format mp4 -o \"{Path.Combine(outputPath, "%(title)s.%(ext)s")}\" \"{playlistUrl}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = psi };
        process.OutputDataReceived += (s, e) => ParseYtDlpOutput(e.Data);
        process.ErrorDataReceived += (s, e) => ParseYtDlpOutput(e.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
    }

    private void ParseYtDlpOutput(string? data)
    {
        if (string.IsNullOrWhiteSpace(data)) return;

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (data.Contains("Destination:"))
            {
                string fileName = data.Replace("Destination:", "").Trim();
                Downloads.Add(new DownloadItem { FileName = Path.GetFileName(fileName), Progress = 0 });
            }

            var m = Regex.Match(data, @"\[\s*download\s*\]\s+(\d{1,3}(?:\.\d+)?)%");
            if (m.Success && double.TryParse(m.Groups[1].Value, out var percent))
            {
                if (Downloads.Count > 0)
                {
                    int progressValue = (int)Math.Round(percent);
                    Downloads.Last().Progress = Math.Min(100, progressValue);
                }
            }

        });
    }

}
