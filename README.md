# YouTubeDownloadPlaylist
Got it ✅
Here’s a **full English explanation** of both projects (the **WPF GUI app** and the **Console app**) you shared.

---

# 📌 Project Summary: YouTube Downloader (WPF + Console)

This solution contains **two different implementations** of a YouTube playlist downloader built in **C#**. Both versions use the external tool **yt-dlp.exe** (a powerful open-source downloader) to fetch and download videos, but they differ in **user interface** and **features**.

---

## 🖼️ WPF Project (Graphical User Interface)

### Purpose

The WPF project provides a **user-friendly desktop application** to download YouTube playlists. It allows users to select a destination folder and download videos with progress tracking.

### Key Features

1. **Browse Folder**

   * A dialog lets users select the output folder for downloads.
   * The chosen folder path is displayed in a text box.

2. **Start Download**

   * Users paste a playlist URL into a text box.
   * When clicking **Download**, the app validates the input and starts downloading with `yt-dlp`.

3. **Download Management**

   * Each file being downloaded is tracked in an `ObservableCollection<DownloadItem>`.
   * The UI shows the file name and download progress.

4. **Progress Updates**

   * The app parses output lines from `yt-dlp` in real time.
   * If it detects `"Destination:"`, it adds a new item to the list.
   * If it detects `"[download] 45%"`, it updates the progress bar.

5. **Status Feedback**

   * Status text is updated ("Starting download…", "Finished!") for clear feedback.

### Technical Notes

* Uses **async/await** with `Process.WaitForExitAsync` for non-blocking execution.
* Redirects **stdout** and **stderr** from `yt-dlp` to capture progress information.
* Uses WPF data binding (`ObservableCollection`) to update the UI automatically.

**In short:**
This is a simple **GUI-based YouTube playlist downloader** that allows folder selection, URL input, and shows real-time progress for each video.

---

## 💻 Console Project (Command-Line Application)

### Purpose

The Console project provides a **text-based interface** with **more advanced features** like format selection, playlist metadata, and automatic internet checking.

### Key Features

1. **Playlist Input**

   * Prompts the user to enter a playlist URL.
   * Validates that the URL is not empty.

2. **Fetch Playlist Title**

   * Calls `yt-dlp` with JSON output to retrieve the playlist title.
   * Creates a dedicated download folder named after the playlist.

3. **Available Formats**

   * Extracts all available formats from the **first video in the playlist**.
   * Shows detailed format information (resolution, codec, bitrate, file size).
   * Lets the user select the preferred format interactively.

4. **Download Execution**

   * Downloads the **entire playlist** in the chosen format.
   * If the format is video-only, it automatically merges the best available audio.

5. **Progress Bar**

   * Displays a **real-time progress bar** in the console, using percentage blocks.
   * Shows the file being downloaded.

6. **Internet Connectivity Check**

   * Before starting, it ensures an internet connection is available by pinging `8.8.8.8`.
   * If not available, it waits and retries every 10 seconds.

7. **Looping**

   * After finishing, it asks if the user wants to download another playlist (`y/n`).

### Technical Notes

* Uses **ProcessStartInfo** to run `yt-dlp.exe` with proper arguments.
* Parses JSON with **System.Text.Json** to extract playlist and format metadata.
* Dynamically builds safe folder names by replacing invalid characters.
* Implements **async/await** for efficient non-blocking downloads.

**In short:**
This is a **feature-rich console downloader** that supports format selection, playlist metadata, progress bars, internet checks, and interactive multiple downloads.

---

## 🆚 Differences Between WPF and Console Versions

| Feature            | WPF Project                   | Console Project                         |
| ------------------ | ----------------------------- | --------------------------------------- |
| User Interface     | Graphical (WPF window)        | Text-based console                      |
| Folder Selection   | File dialog (GUI)             | Auto-created folder from playlist title |
| Format Choice      | Always best video+audio (mp4) | User can choose resolution/format       |
| Progress Tracking  | GUI list with percentages     | Console progress bar                    |
| Internet Check     | No                            | Yes (pings Google DNS)                  |
| Multiple Downloads | One at a time                 | Can repeat multiple playlists           |
| Complexity         | Simpler                       | More advanced                           |

---

✅ **Conclusion**

* The **WPF project** is designed for **ease of use**, ideal for users who want a simple point-and-click app.
* The **Console project** is designed for **power users**, giving more control over formats, connectivity, and metadata.

---

Do you want me to also **combine both explanations into a single professional project documentation** (like a README file for GitHub)?
