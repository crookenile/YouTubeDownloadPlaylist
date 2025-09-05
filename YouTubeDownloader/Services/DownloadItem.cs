using System.ComponentModel;

namespace YouTubeDownloader.Services;

public class DownloadItem : INotifyPropertyChanged
{
    private int _progress;

    public string FileName { get; set; } = "";

    public int Progress
    {
        get => _progress;
        set
        {
            if (_progress != value)
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
