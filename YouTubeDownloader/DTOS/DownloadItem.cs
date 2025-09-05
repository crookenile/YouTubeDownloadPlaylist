using System.ComponentModel;

namespace YouTubeDownloader;

public class DownloadItem : INotifyPropertyChanged
{
    private double progress;
    public string FileName { get; set; } = "";

    public double Progress
    {
        get => progress;
        set
        {
            progress = value;
            OnPropertyChanged(nameof(Progress));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
