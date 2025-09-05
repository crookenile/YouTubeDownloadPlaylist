using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;

// Allow UTF8
Console.OutputEncoding = Encoding.UTF8;

do
{
    // 1) Ask user for playlist URL
    Console.WriteLine("📥 Enter YouTube Playlist URL:");
    string playlistUrl = Console.ReadLine()?.Trim() ?? "";
    if (string.IsNullOrWhiteSpace(playlistUrl))
    {
        Console.WriteLine("❌ Invalid URL.");
        return;
    }

    // 2) Fetch playlist title
    Console.WriteLine("🔍 Fetching playlist title...");
    string playlistTitle = await GetPlaylistTitle(playlistUrl);
    if (string.IsNullOrWhiteSpace(playlistTitle))
    {
        Console.WriteLine("❌ Could not fetch playlist title.");
        return;
    }

    // 3) Create output folder
    string folder = Path.Combine(Directory.GetCurrentDirectory(), MakeValidFileName(playlistTitle));
    Directory.CreateDirectory(folder);
    Console.WriteLine($"📁 Download folder: {folder}");

    // 4) Fetch available formats
    Console.WriteLine("🎞️ Fetching available formats...");
    var formats = await GetAvailableFormats(playlistUrl);
    if (formats.Count == 0)
    {
        Console.WriteLine("❌ No formats found.");
        return;
    }

    // 5) Show formats
    Console.WriteLine("\n📌 Available formats:");
    for (int i = 0; i < formats.Count; i++)
        Console.WriteLine($"{i + 1}. {formats[i].Label}");

    // 6) Let user choose
    int selected = -1;
    while (selected < 1 || selected > formats.Count)
    {
        Console.Write("🔢 Enter format number: ");
        int.TryParse(Console.ReadLine(), out selected);
    }

    var chosen = formats[selected - 1];
    Console.WriteLine($"\n🚀 Downloading all videos in format: {chosen.FormatCode} ({(chosen.IsVideoOnly ? "video-only + bestaudio" : "video+audio")})");

    // 7) Wait until internet available
    await WaitForInternetAsync();

    // 8) Download playlist
    await DownloadPlaylist(playlistUrl, folder, chosen);

    Console.WriteLine("\n✅ Done! All videos downloaded.");

    Console.WriteLine("\nDo you want to download another playlist? (y/n): ");
}
while (Console.ReadLine()?.Trim().ToLower() == "y");

// ================= Helpers =================

async Task<string> RunAndCaptureAsync(string file, string args)
{
    var psi = new ProcessStartInfo
    {
        FileName = file,
        Arguments = args,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8
    };

    using var p = Process.Start(psi)!;
    string stdout = await p.StandardOutput.ReadToEndAsync();
    string stderr = await p.StandardError.ReadToEndAsync();
    await p.WaitForExitAsync();

    if (string.IsNullOrWhiteSpace(stdout) && !string.IsNullOrWhiteSpace(stderr))
        Console.WriteLine($"⚠️ yt-dlp stderr: {FirstLines(stderr, 6)}");

    return stdout;
}

string FirstLines(string text, int lines)
{
    var arr = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    return string.Join(Environment.NewLine, arr.Take(lines));
}

async Task<string> GetPlaylistTitle(string url)
{
    string jsonLines = await RunAndCaptureAsync("yt-dlp.exe", $"-j --flat-playlist \"{url}\"");
    foreach (var line in jsonLines.Split('\n'))
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        try
        {
            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.TryGetProperty("playlist_title", out var t) && t.ValueKind == JsonValueKind.String)
                return t.GetString() ?? "";
        }
        catch { }
    }
    return "";
}

async Task<string> GetFirstVideoUrl(string playlistUrl)
{
    string id = (await RunAndCaptureAsync("yt-dlp.exe", $"--get-id --skip-download --playlist-items 1 \"{playlistUrl}\""))
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "";

    return string.IsNullOrWhiteSpace(id) ? "" : $"https://www.youtube.com/watch?v={id}";
}

async Task<List<FormatInfo>> GetAvailableFormats(string playlistUrl)
{
    var list = new List<FormatInfo>();
    string firstVideo = await GetFirstVideoUrl(playlistUrl);
    if (string.IsNullOrWhiteSpace(firstVideo)) return list;

    string json = await RunAndCaptureAsync("yt-dlp.exe", $"-J --no-playlist \"{firstVideo}\"");
    if (string.IsNullOrWhiteSpace(json)) return list;

    using var doc = JsonDocument.Parse(json);
    if (!doc.RootElement.TryGetProperty("formats", out var formatsEl) || formatsEl.ValueKind != JsonValueKind.Array)
        return list;

    var seen = new HashSet<string>();

    foreach (var f in formatsEl.EnumerateArray())
    {
        string id = GetStr(f, "format_id") ?? "";
        if (string.IsNullOrWhiteSpace(id) || !seen.Add(id)) continue;

        string vcodec = GetStr(f, "vcodec") ?? "";
        string acodec = GetStr(f, "acodec") ?? "";

        if (string.Equals(vcodec, "none", StringComparison.OrdinalIgnoreCase))
            continue;

        string ext = GetStr(f, "ext") ?? "";
        int height = GetInt(f, "height") ?? 0;
        int fps = (int?)GetDouble(f, "fps") ?? 0;
        double? tbr = GetDouble(f, "tbr");
        long? size = GetLong(f, "filesize") ?? GetLong(f, "filesize_approx");

        bool isVideoOnly = string.Equals(acodec, "none", StringComparison.OrdinalIgnoreCase);

        string res = height > 0 ? $"{height}p" : "?p";
        string fpsPart = fps > 0 ? $"/{fps}fps" : "";
        string brPart = tbr.HasValue ? $" | ~{Math.Round(tbr.Value)}kbps" : "";
        string sizePart = size.HasValue ? $" | ~{Math.Round(size.Value / 1024d / 1024d)}MB" : "";

        string label = $"{id} | {ext} | {res}{fpsPart} | v:{Short(vcodec)} a:{Short(acodec)}{brPart}{sizePart}";
        list.Add(new FormatInfo
        {
            FormatCode = id,
            Label = label,
            IsVideoOnly = isVideoOnly,
            SortKey = (height, isVideoOnly ? 0 : 1, fps)
        });
    }

    list = list.OrderByDescending(x => x.SortKey.height)
               .ThenByDescending(x => x.SortKey.Item2)
               .ThenByDescending(x => x.SortKey.fps)
               .ToList();

    return list;

    static string? GetStr(JsonElement e, string name)
        => e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    static int? GetInt(JsonElement e, string name)
        => e.TryGetProperty(name, out var p) && p.TryGetInt32(out var v) ? v : (int?)null;

    static long? GetLong(JsonElement e, string name)
        => e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var v) ? v : (long?)null;

    static double? GetDouble(JsonElement e, string name)
        => e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetDouble(out var v) ? v : (double?)null;

    static string Short(string? s) => string.IsNullOrWhiteSpace(s) ? "?" : (s.Length > 20 ? s[..20] + "…" : s);
}

async Task DownloadPlaylist(string playlistUrl, string outputPath, FormatInfo format)
{
    string formatArg = format.IsVideoOnly ? $"{format.FormatCode}+bestaudio" : format.FormatCode;

    var psi = new ProcessStartInfo
    {
        FileName = "yt-dlp.exe",
        Arguments = $"-c -f {formatArg} --merge-output-format mp4 --yes-playlist -o \"{Path.Combine(outputPath, "%(title)s.%(ext)s")}\" \"{playlistUrl}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    var process = new Process { StartInfo = psi };

    process.OutputDataReceived += (_, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
            DisplayProgress(e.Data);
    };
    process.ErrorDataReceived += (_, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
            DisplayProgress(e.Data);
    };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    await process.WaitForExitAsync();
}

void DisplayProgress(string data)
{
    var m = Regex.Match(data, @"\[\s*download\s*\]\s+(\d{1,3}(?:\.\d+)?)%");
    if (m.Success && double.TryParse(m.Groups[1].Value, out var percent))
    {
        int blocks = (int)(percent / 4);
        Console.Write($"\r📦 [{new string('█', blocks)}{new string(' ', 25 - blocks)}] {percent:0.0}%   ");
        Console.ForegroundColor = ConsoleColor.Green;
        if (percent >= 100) Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
    }
    else if (data.Contains("Destination:"))
        Console.WriteLine($"\n🎬 {data}");
}

string MakeValidFileName(string name)
{
    foreach (char c in Path.GetInvalidFileNameChars())
        name = name.Replace(c, '_');
    return name;
}

// Internet check
async Task WaitForInternetAsync()
{
    while (!IsInternetAvailable())
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("❌ No internet connection. Retrying in 10 seconds...");
        Console.ResetColor();
        await Task.Delay(10000);
    }
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ Internet connection available.");
    Console.ResetColor();
}

bool IsInternetAvailable()
{
    try
    {
        using var ping = new Ping();
        var reply = ping.Send("8.8.8.8", 3000);
        return reply.Status == IPStatus.Success;
    }
    catch { return false; }
}
