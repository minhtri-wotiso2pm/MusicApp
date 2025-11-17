# ?? WOTISO MUSIC PLAYER - TÀI LI?U D? ÁN

> **Music Player Desktop Application** - ?ng d?ng nghe nh?c desktop ???c xây d?ng b?ng WPF (.NET 9)

---

## ?? M?C L?C

1. [T?ng quan d? án](#t?ng-quan-d?-án)
2. [Ki?n trúc h? th?ng](#ki?n-trúc-h?-th?ng)
3. [Công ngh? s? d?ng](#công-ngh?-s?-d?ng)
4. [C?u trúc th? m?c](#c?u-trúc-th?-m?c)
5. [Các thành ph?n chính](#các-thành-ph?n-chính)
6. [Lu?ng d? li?u](#lu?ng-d?-li?u)
7. [Tính n?ng](#tính-n?ng)
8. [H??ng d?n s? d?ng](#h??ng-d?n-s?-d?ng)
9. [H??ng d?n phát tri?n](#h??ng-d?n-phát-tri?n)

---

## ?? T?NG QUAN D? ÁN

### **Mô t?:**
Wotiso Music Player là ?ng d?ng desktop cho phép ng??i dùng:
- Qu?n lý th? vi?n nh?c cá nhân
- T?o và qu?n lý playlists
- Phát nh?c v?i giao di?n ??p m?t
- Tìm ki?m bài hát và playlist
- L?u tr? d? li?u d?ng JSON (portable)

### **??i t??ng s? d?ng:**
- Ng??i dùng cá nhân mu?n qu?n lý nh?c trên máy tính
- Không c?n ??ng nh?p, app ch?y local

### **?i?m ??c bi?t:**
- ? Giao di?n hi?n ??i v?i animation
- ?? L?u tr? JSON thay vì database (d? backup)
- ?? Mini disc quay khi ch?n bài
- ?? Tìm ki?m real-time
- ?? Visualizer nh?c ??ng

---

## ??? KI?N TRÚC H? TH?NG

### **Mô hình 3 l?p (3-Tier Architecture):**

```
???????????????????????????????????????????
?   PRESENTATION LAYER (UI)               ?
?   - MainWindow.xaml                     ?
?   - InputDialog.xaml                    ?
?   - WPF Controls & Animations           ?
???????????????????????????????????????????
               ?
               ?
???????????????????????????????????????????
?   BUSINESS LOGIC LAYER (BLL)            ?
?   - MusicService                        ?
?   - PlaylistService                     ?
?   - UserService (deprecated)            ?
???????????????????????????????????????????
               ?
               ?
???????????????????????????????????????????
?   DATA ACCESS LAYER (DAL)               ?
?   - JsonDataStore                       ?
?   - JsonSongRepository                  ?
?   - JsonPlaylistRepository              ?
?   - Entities (Song, Playlist, etc.)     ?
???????????????????????????????????????????
               ?
               ?
        [musicapp_data.json]
```

### **Gi?i thích:**

#### **1. PRESENTATION LAYER (L?p giao di?n)**
- **Trách nhi?m:** Hi?n th? UI, x? lý t??ng tác ng??i dùng
- **Công ngh?:** WPF (Windows Presentation Foundation)
- **Thành ph?n chính:**
  - `MainWindow.xaml` - C?a s? chính
  - `InputDialog.xaml` - Dialog nh?p tên playlist
  - Animations - ??a quay, visualizer

#### **2. BUSINESS LOGIC LAYER (L?p x? lý logic)**
- **Trách nhi?m:** X? lý nghi?p v?, validation, business rules
- **Services:**
  - `MusicService` - Qu?n lý bài hát
  - `PlaylistService` - Qu?n lý playlist

#### **3. DATA ACCESS LAYER (L?p truy c?p d? li?u)**
- **Trách nhi?m:** ??c/ghi d? li?u t? JSON
- **Repositories:**
  - `JsonSongRepository` - CRUD bài hát
  - `JsonPlaylistRepository` - CRUD playlist
- **Storage:**
  - `JsonDataStore` - Core engine l?u/??c JSON

---

## ??? CÔNG NGH? S? D?NG

### **Framework & Platform:**
- **.NET 9** - Framework chính
- **WPF** - Windows Presentation Foundation
- **C# 13.0** - Ngôn ng? l?p trình

### **Th? vi?n:**
- **System.Text.Json** - Serialize/Deserialize JSON (built-in)
- **MediaElement** - Phát nh?c/video
- **DispatcherTimer** - Update progress bar

### **Tools:**
- **Visual Studio 2022** - IDE
- **Git** - Version control

### **L?u tr?:**
- **JSON File** - `musicapp_data.json`
- **???ng d?n:** `D:\FPT\FA25\PRN212\Project3\MusicApp\Data\`

---

## ?? C?U TRÚC TH? M?C

```
MusicApp/
?
??? Wotiso.MusicApp/                    # Presentation Layer (UI)
?   ??? MainWindow.xaml                 # Giao di?n chính
?   ??? MainWindow.xaml.cs              # Code-behind chính
?   ??? InputDialog.xaml                # Dialog nh?p tên playlist
?   ??? InputDialog.xaml.cs             # Code-behind dialog
?   ??? App.xaml                        # Application definition
?   ??? App.xaml.cs                     # Application startup
?   ??? appsettings.json                # C?u hình app
?
??? Wotiso.MusicApp.BLL/                # Business Logic Layer
?   ??? Services/
?       ??? MusicService.cs             # Service qu?n lý nh?c
?       ??? PlaylistService.cs          # Service qu?n lý playlist
?       ??? UserService.cs              # (Deprecated)
?
??? Wotiso.MusicApp.DAL/                # Data Access Layer
?   ??? Entities/                       # Data Models
?   ?   ??? Song.cs                     # Model bài hát
?   ?   ??? Playlist.cs                 # Model playlist
?   ?   ??? PlaylistSong.cs             # Junction table
?   ?   ??? User.cs                     # (Deprecated)
?   ?   ??? DownloadHistory.cs          # (Deprecated)
?   ?
?   ??? Storage/                        # JSON Storage
?   ?   ??? JsonDataStore.cs            # Core JSON engine
?   ?
?   ??? Repositories/                   # Data repositories
?   ?   ??? JsonSongRepository.cs       # Song CRUD (JSON)
?   ?   ??? JsonPlaylistRepository.cs   # Playlist CRUD (JSON)
?   ?   ??? SongRepo.cs                 # (Old - SQLite)
?   ?   ??? PlaylistRepo.cs             # (Old - SQLite)
?   ?
?   ??? MusicPlayerDbContext.cs         # (Old - EF Core)
?
??? Data/                               # Folder l?u JSON
?   ??? musicapp_data.json              # File d? li?u chính
?
??? Docs/                               # Documentation
    ??? JSON_STORAGE_GUIDE.md           # H??ng d?n JSON storage
    ??? CHANGE_JSON_PATH_GUIDE.md       # ??i ???ng d?n JSON
    ??? SEARCH_FEATURE_GUIDE.md         # Tính n?ng tìm ki?m
```

---

## ?? CÁC THÀNH PH?N CHÍNH

### **1. PRESENTATION LAYER**

#### **MainWindow.xaml.cs**
```csharp
// C?a s? chính c?a ?ng d?ng
public partial class MainWindow : Window
{
    // Services
    private readonly MusicService _musicService;
    private readonly PlaylistService _playlistService;
    
    // Data
    private List<Song> _songs;              // Danh sách bài hát hi?n t?i
    private List<Playlist> _playlists;      // Danh sách playlist
    
    // Player state
    private int _currentIndex;              // Bài ?ang phát
    private bool _isPaused;                 // ?ang pause?
    private bool _isLoop;                   // Loop bài?
    private bool _isShuffle;                // Shuffle?
    
    // Animation
    private Storyboard _visualizerStoryboard; // Animation visualizer
}
```

**Các ch?c n?ng chính:**
- ? Hi?n th? danh sách bài hát và playlist
- ? Phát nh?c v?i MediaElement
- ? Qu?n lý playlist (t?o, xóa, thêm bài)
- ? Tìm ki?m bài hát và playlist
- ? Animation (??a quay, visualizer)

#### **InputDialog.xaml.cs**
```csharp
// Dialog ??n gi?n ?? nh?p tên playlist
public partial class InputDialog : Window
{
    public string InputText { get; set; }
}
```

---

### **2. BUSINESS LOGIC LAYER**

#### **MusicService.cs**
```csharp
// Service qu?n lý bài hát
public class MusicService
{
    private readonly JsonSongRepository _repo;
    
    // L?y t?t c? bài hát
    public List<Song> GetAllSongs();
    
    // Thêm nhi?u bài t? file
    public List<Song> LoadLocalSongsFromFiles(List<string> files);
    
    // Xóa bài hát
    public void DeleteSong(int id);
    
    // Qu?n lý favorite
    public void AddToFavorites(Song song);
    public void RemoveFromFavorites(Song song);
}
```

#### **PlaylistService.cs**
```csharp
// Service qu?n lý playlist
public class PlaylistService
{
    private readonly JsonPlaylistRepository _repo;
    
    // L?y t?t c? playlist
    public List<Playlist> GetAllPlaylists();
    
    // T?o playlist m?i
    public Playlist CreateNewPlaylist(string name);
    
    // Xóa playlist
    public void DeletePlaylist(int id);
    
    // Thêm/xóa bài trong playlist
    public void AddSongToPlaylist(int playlistId, int songId);
    public void RemoveSongFromPlaylist(int playlistId, int songId);
    
    // L?y bài trong playlist
    public List<Song> GetSongsForPlaylist(int playlistId);
}
```

---

### **3. DATA ACCESS LAYER**

#### **JsonDataStore.cs** (Core)
```csharp
// Engine chính cho JSON storage
public class JsonDataStore
{
    private string _dataFilePath;           // ???ng d?n file JSON
    private MusicAppData _data;             // Data trong memory
    
    // ??c/ghi JSON
    private void LoadData();
    public void SaveData();
    
    // CRUD Songs
    public List<Song> GetAllSongs();
    public void AddSong(Song song);
    public void BulkAddSongs(List<Song> songs);
    public void UpdateSong(Song song);
    public void DeleteSong(int id);
    
    // CRUD Playlists
    public List<Playlist> GetAllPlaylists();
    public Playlist CreatePlaylist(Playlist playlist);
    public void DeletePlaylist(int id);
    public void AddSongToPlaylist(int playlistId, int songId);
    public void RemoveSongFromPlaylist(int playlistId, int songId);
}
```

#### **Entities (Data Models)**

**Song.cs** - Bài hát
```csharp
public class Song
{
    public int SongId { get; set; }         // ID t? ??ng t?ng
    public string Title { get; set; }       // Tên bài hát
    public string FilePath { get; set; }    // ???ng d?n file
    public string FileType { get; set; }    // .mp3, .wav, etc.
    public bool IsFavorite { get; set; }    // ?ánh d?u yêu thích
    public DateTime CreatedAt { get; set; } // Ngày thêm
}
```

**Playlist.cs** - Danh sách phát
```csharp
public class Playlist
{
    public int PlaylistId { get; set; }     // ID t? ??ng t?ng
    public string PlaylistName { get; set; }// Tên playlist
    public List<int> SongIds { get; set; }  // Danh sách ID bài hát
    public DateTime CreatedAt { get; set; } // Ngày t?o
}
```

---

## ?? LU?NG D? LI?U

### **1. Kh?i ??ng ?ng d?ng:**
```
User kh?i ??ng app
    ?
App.xaml.cs OnStartup()
    ?
T?o JsonDataStore (??c musicapp_data.json)
    ?
T?o Repositories (JsonSongRepository, JsonPlaylistRepository)
    ?
T?o Services (MusicService, PlaylistService)
    ?
T?o MainWindow và truy?n Services
    ?
MainWindow load playlists và songs
    ?
Hi?n th? UI
```

### **2. Thêm bài hát:**
```
User click "Add Songs"
    ?
MainWindow.SelectFiles_Click()
    ?
M? OpenFileDialog ch?n file
    ?
Validate t?ng file (t?n t?i, không r?ng)
    ?
MusicService.LoadLocalSongsFromFiles()
    ?
JsonSongRepository.BulkAddSongs()
    ?
JsonDataStore.BulkAddSongs() + SaveData()
    ?
Ghi vào musicapp_data.json
    ?
Reload UI hi?n th? bài m?i
```

### **3. T?o playlist:**
```
User click "T?o Playlist M?i"
    ?
Hi?n th? InputDialog nh?p tên
    ?
PlaylistService.CreateNewPlaylist(name)
    ?
JsonPlaylistRepository.CreatePlaylist()
    ?
JsonDataStore.CreatePlaylist() + SaveData()
    ?
Ghi vào musicapp_data.json
    ?
Reload danh sách playlist
```

### **4. Phát nh?c:**
```
User double-click bài hát
    ?
MainWindow.PlaySong(song)
    ?
Ki?m tra file t?n t?i
    ?
Stop bài c? (n?u có)
    ?
MediaElement.Source = new Uri(song.FilePath)
    ?
MediaElement.Play()
    ?
Start DispatcherTimer c?p nh?t progress
    ?
Start animations (??a quay, visualizer)
```

---

## ? TÍNH N?NG

### **1. Qu?n lý nh?c**
- ? Thêm nhi?u bài t? local files
- ? Xóa bài kh?i th? vi?n
- ? T? ??ng l?y tên file làm title
- ? H? tr?: mp3, wav, wma, aac, m4a, mp4, avi, wmv, mov

### **2. Qu?n lý Playlist**
- ? T?o playlist m?i
- ? Xóa playlist
- ? Thêm bài vào playlist (context menu)
- ? Xóa bài kh?i playlist
- ? Xem bài trong playlist

### **3. Phát nh?c**
- ? Play/Pause/Next/Previous
- ? Progress bar kéo th?
- ? Volume control (horizontal slider)
- ? Loop m?t bài
- ? Shuffle (phát ng?u nhiên)
- ? Hi?n th? th?i gian hi?n t?i/t?ng

### **4. Tìm ki?m**
- ? Tìm bài hát theo tên (real-time)
- ? Tìm playlist theo tên
- ? Không phân bi?t hoa th??ng
- ? Filter t?c thì khi gõ

### **5. Giao di?n**
- ? Theme t?i (Dark Mode)
- ? Animation ??a nh?c quay
- ? Visualizer nh?c (5 thanh nh?p nhô)
- ? Gradient buttons
- ? Glass effect cards
- ? Responsive design

### **6. L?u tr?**
- ? JSON file storage (không c?n database)
- ? Auto-save m?i thay ??i
- ? Portable (copy folder là xong)
- ? D? backup (1 file JSON)

---

## ?? H??NG D?N S? D?NG

### **1. Thêm nh?c vào th? vi?n:**
1. Click nút **"?? Add Songs"**
2. Ch?n m?t ho?c nhi?u file nh?c
3. App s? t? ??ng thêm vào th? vi?n

### **2. T?o playlist:**
1. Click nút **"? T?o Playlist M?i"**
2. Nh?p tên playlist
3. Click OK

### **3. Thêm bài vào playlist:**
1. Right-click vào bài hát
2. Ch?n **"Thêm vào Playlist..."**
3. Ch?n playlist mu?n thêm

### **4. Phát nh?c:**
- **Double-click** bài hát ?? phát
- Ho?c ch?n bài r?i click **? Play**
- **? Pause** ?? t?m d?ng
- **? Next** / **? Previous** ?? chuy?n bài

### **5. Tìm ki?m:**
- Gõ tên bài vào ô **"?? Tìm bài hát..."**
- Ho?c tìm playlist trong ô **"?? Tìm ki?m..."**

### **6. Xóa bài/playlist:**
- **Xóa bài:** Ch?n bài ? Click **"?? Delete"**
- **Xóa playlist:** Right-click playlist ? **"Xóa Playlist Này"**

---

## ????? H??NG D?N PHÁT TRI?N

### **1. Yêu c?u h? th?ng:**
- **.NET 9 SDK**
- **Visual Studio 2022** (ho?c VS Code + C# extension)
- **Windows 10/11**

### **2. Clone và build:**
```bash
# Clone project
git clone <repository-url>
cd MusicApp

# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run --project Wotiso.MusicApp
```

### **3. C?u trúc Solution:**
```
MusicApp.sln
??? Wotiso.MusicApp (WPF App)
??? Wotiso.MusicApp.BLL (Class Library)
??? Wotiso.MusicApp.DAL (Class Library)
```

### **4. Thêm tính n?ng m?i:**

#### **Thêm ch?c n?ng ? UI:**
1. M? `MainWindow.xaml`
2. Thêm Button/Control m?i
3. T?o event handler trong `MainWindow.xaml.cs`
4. G?i Service ?? x? lý logic

#### **Thêm business logic:**
1. M? `MusicService.cs` ho?c `PlaylistService.cs`
2. Thêm method m?i
3. G?i Repository ?? truy c?p d? li?u

#### **Thêm ch?c n?ng l?u tr?:**
1. M? `JsonDataStore.cs`
2. Thêm method CRUD m?i
3. Update `MusicAppData` n?u c?n thêm property

### **5. Debug:**
- **Log file:** `D:\musicapp_debug.log`
- **Breakpoints:** ??t trong Visual Studio
- **Live debugging:** Xem Debug Output window

### **6. Best Practices:**

#### **Code style:**
```csharp
// ? GOOD: Comment rõ ràng
/// <summary>
/// Phát m?t bài hát c? th?
/// </summary>
/// <param name="song">Bài hát c?n phát</param>
private void PlaySong(Song song)
{
    // Validate
    if (song == null || !File.Exists(song.FilePath))
        return;
        
    // Logic
    mediaPlayer.Source = new Uri(song.FilePath);
    mediaPlayer.Play();
}

// ? BAD: Không comment, không validate
private void PlaySong(Song song)
{
    mediaPlayer.Source = new Uri(song.FilePath);
    mediaPlayer.Play();
}
```

#### **Error handling:**
```csharp
// ? GOOD: Try-catch v?i log
try
{
    _musicService.DeleteSong(songId);
    LogDebug($"Song {songId} deleted successfully");
}
catch (Exception ex)
{
    LogDebug($"Error deleting song: {ex.Message}");
    MessageBox.Show($"L?i: {ex.Message}", "L?i", 
        MessageBoxButton.OK, MessageBoxImage.Error);
}

// ? BAD: Không x? lý l?i
_musicService.DeleteSong(songId);
```

#### **UI Threading:**
```csharp
// ? GOOD: Async/await cho heavy tasks
private async void LoadSongs_Click(object sender, RoutedEventArgs e)
{
    this.IsEnabled = false;
    this.Cursor = Cursors.Wait;
    
    var songs = await Task.Run(() => 
        _musicService.LoadLocalSongsFromFiles(files));
    
    this.IsEnabled = true;
    this.Cursor = Cursors.Arrow;
}

// ? BAD: Block UI thread
private void LoadSongs_Click(object sender, RoutedEventArgs e)
{
    var songs = _musicService.LoadLocalSongsFromFiles(files); // FREEZE!
}
```

---

## ?? D? LI?U JSON

### **C?u trúc file `musicapp_data.json`:**
```json
{
  "Songs": [
    {
      "SongId": 1,
      "Title": "Bài hát 1",
      "FilePath": "D:\\Music\\song1.mp3",
      "FileType": ".mp3",
      "IsFavorite": false,
      "CreatedAt": "2024-01-15T10:30:00"
    },
    {
      "SongId": 2,
      "Title": "Bài hát 2",
      "FilePath": "D:\\Music\\song2.mp3",
      "FileType": ".mp3",
      "IsFavorite": true,
      "CreatedAt": "2024-01-15T10:35:00"
    }
  ],
  "Playlists": [
    {
      "PlaylistId": 1,
      "PlaylistName": "Yêu thích",
      "SongIds": [1, 2],
      "CreatedAt": "2024-01-15T11:00:00"
    }
  ],
  "NextSongId": 3,
  "NextPlaylistId": 2
}
```

### **V? trí file:**
```
D:\FPT\FA25\PRN212\Project3\MusicApp\Data\musicapp_data.json
```

---

## ?? TROUBLESHOOTING

### **1. App không kh?i ??ng:**
- Ki?m tra .NET 9 SDK ?ã cài ch?a
- Xem log: `D:\musicapp_debug.log`
- Build l?i: `dotnet clean && dotnet build`

### **2. Không phát ???c nh?c:**
- Ki?m tra file nh?c có t?n t?i không
- Ki?m tra codec (Windows Media Player có m? ???c không?)
- Xem `FilePath` trong JSON có ?úng không

### **3. M?t d? li?u:**
- Ki?m tra file JSON còn không
- Restore t? backup n?u có
- Check quy?n ghi file

### **4. UI b? ?? (freeze):**
- Có th? ?ang load nhi?u file
- Ch? ho?c restart app
- Ki?m tra log xem ?ang x? lý gì

---

## ?? TÀI LI?U THAM KH?O

### **Trong project:**
- `JSON_STORAGE_GUIDE.md` - H??ng d?n JSON storage
- `CHANGE_JSON_PATH_GUIDE.md` - ??i ???ng d?n l?u JSON
- `SEARCH_FEATURE_GUIDE.md` - Tính n?ng tìm ki?m

### **External:**
- [WPF Documentation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- [.NET 9 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)
- [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)

---

## ?? TEAM & CREDITS

### **Developed by:**
- **Project:** Wotiso Music Player
- **Course:** PRN212 - FPT University
- **Semester:** FA25

### **Contact:**
- **Workspace:** `D:\FPT\FA25\PRN212\Project3\MusicApp\`

---

## ?? CHANGELOG

### **Version 2.0 (Current) - JSON Storage**
- ? Chuy?n t? SQLite sang JSON storage
- ? Thêm tính n?ng tìm ki?m
- ? C?i thi?n UI animations
- ? Fix màn hình ?en khi load file
- ? Thêm visualizer nh?c

### **Version 1.0 - SQLite Storage**
- ? Phát nh?c c? b?n
- ? Qu?n lý playlist
- ? Thêm/xóa bài hát
- ? UI c? b?n

---

## ?? ROADMAP (T??ng lai)

### **Planned Features:**
- [ ] Lyrics hi?n th?
- [ ] Equalizer
- [ ] Keyboard shortcuts
- [ ] Minimize to system tray
- [ ] Export playlist to M3U
- [ ] Cloud sync (Google Drive/Dropbox)
- [ ] Theme customization
- [ ] Multiple languages support

---

## ?? LICENSE

> D? án h?c t?p - FPT University
> Ch? s? d?ng cho m?c ?ích h?c t?p và nghiên c?u

---

**Happy Coding! ???**

---

*Document created: 2024*  
*Last updated: 2024*  
*Version: 2.0*
