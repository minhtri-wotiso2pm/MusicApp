using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Wotiso.MusicApp.BLL.Services;
using Wotiso.MusicApp.DAL.Entities;

namespace Wotiso.MusicApp
{
    public partial class MainWindow : Window
    {
        // SỬA: Quản lý services và user
        private readonly MusicService _musicService;
        private readonly PlaylistService _playlistService;
        private readonly User _currentUser;

        // SỬA: _songs giờ là danh sách đang hiển thị
        private List<Song> _songs = new();
        private List<Playlist> _playlists = new(); // Danh sách playlist của user

        private int _currentIndex = -1;
        private DispatcherTimer _timer;
        private bool _isPaused = false;
        private bool _isLoop = false;
        private bool _isShuffle = false;

        // Biến tạm để biết đang xem playlist nào
        private Playlist _currentViewingPlaylist = null; // null = xem Library

        // NEW: Biến lưu trữ danh sách gốc để filter
        private List<Song> _allSongsInCurrentView = new(); // Tất cả bài hát trong view hiện tại (trước khi filter)
        private List<Playlist> _allPlaylists = new(); // Tất cả playlist (trước khi filter)

        // ==================== LOGGING CHO DEBUG ====================
        /// <summary>
        /// Ghi log để debug khi có lỗi màn hình đen hoặc crash
        /// Log được ghi vào: D:\musicapp_debug.log
        /// Dùng để trace từng bước thực thi và tìm nguyên nhân lỗi
        /// </summary>
        private void LogDebug(string message)
        {
            // Xuất log ra Debug Console của Visual Studio
            Debug.WriteLine($"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - {message}");
            try
            {
                // Ghi log vào file để xem lại sau (không bị mất khi đóng app)
                File.AppendAllText("D:\\musicapp_debug.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}\n");
            }
            catch { } // Bỏ qua lỗi ghi file để không làm crash app
        }

        // ==================== CONSTRUCTOR ====================
        /// <summary>
        /// Constructor nhận 3 tham số từ LoginWindow sau khi đăng nhập thành công
        /// QUAN TRỌNG: Mọi initialization phải được wrap trong try-catch để tránh crash
        /// </summary>
        /// <param name="loggedInUser">User vừa đăng nhập</param>
        /// <param name="musicService">Service quản lý bài hát</param>
        /// <param name="playlistService">Service quản lý playlist</param>
        public MainWindow(User loggedInUser, MusicService musicService, PlaylistService playlistService)
        {
            try
            {
                LogDebug("===== MainWindow Constructor START =====");
                LogDebug($"User: {loggedInUser?.UserName ?? "NULL"}");
                
                LogDebug("Step 1: InitializeComponent...");
                InitializeComponent();
                LogDebug("Step 1: InitializeComponent DONE");

                LogDebug("Step 2: Set services...");
                _currentUser = loggedInUser;
                _musicService = musicService;
                _playlistService = playlistService;
                LogDebug("Step 2: Services set DONE");

                LogDebug("Step 3: LoadUserPlaylists...");
                LoadUserPlaylists();
                LogDebug("Step 3: LoadUserPlaylists DONE");
                
                LogDebug("Step 4: LoadLibrarySongs...");
                LoadLibrarySongs();
                LogDebug("Step 4: LoadLibrarySongs DONE");

                LogDebug("Step 5: Initialize Timer...");
                _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                _timer.Tick += Timer_Tick;
                LogDebug("Step 5: Timer initialized DONE");

                LogDebug("Step 6: Setup Volume...");
                VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
                mediaPlayer.Volume = VolumeSlider.Value;
                LogDebug($"Step 6: Volume set to {VolumeSlider.Value} DONE");

                LogDebug("Step 7: Setup Progress Slider...");
                ProgressSlider.ValueChanged += ProgressSlider_ValueChanged;
                ProgressSlider.PreviewMouseDown += ProgressSlider_PreviewMouseDown;
                ProgressSlider.PreviewMouseUp += ProgressSlider_PreviewMouseUp;
                LogDebug("Step 7: Progress Slider setup DONE");

                LogDebug("Step 8: UpdateEmptyState...");
                UpdateEmptyState();
                LogDebug("Step 8: UpdateEmptyState DONE");

                LogDebug("===== MainWindow Constructor COMPLETED SUCCESSFULLY =====");
            }
            catch (Exception ex)
            {
                LogDebug($"!!!!! EXCEPTION in Constructor: {ex.Message}");
                LogDebug($"Stack Trace: {ex.StackTrace}");
                LogDebug($"Inner Exception: {ex.InnerException?.Message}");
                
                MessageBox.Show($"CRITICAL ERROR in MainWindow Constructor:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}\n\nCheck log file: D:\\musicapp_debug.log",
                    "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // ==================== LOAD PLAYLISTS ====================
        /// <summary>
        /// Tải tất cả playlist của user từ database
        /// - Thêm item "Tất cả bài hát (Thư viện)" với PlaylistId = -1 làm mặc định
        /// - Load tất cả playlist của user vào ListBox bên trái
        /// - Cập nhật context menu "Thêm vào playlist"
        /// - LƯU TẤT CẢ PLAYLIST để dùng cho search filter
        /// </summary>
        private void LoadUserPlaylists()
        {
            try
            {
                LogDebug("LoadUserPlaylists: Getting playlists from service...");
                _playlists = _playlistService.GetPlaylistsForUser(_currentUser.UserId);
                LogDebug($"LoadUserPlaylists: Got {_playlists.Count} playlists");

                // NEW: Lưu tất cả playlist để filter
                _allPlaylists = new List<Playlist>();
                _allPlaylists.Add(new Playlist { PlaylistId = -1, PlaylistName = "Tất cả bài hát (Thư viện)" });
                _allPlaylists.AddRange(_playlists);

                LogDebug("LoadUserPlaylists: Clearing PlaylistList...");
                PlaylistList.ItemsSource = null;
                PlaylistList.Items.Clear();
                
                LogDebug("LoadUserPlaylists: Adding default library item...");
                PlaylistList.Items.Add(new Playlist { PlaylistId = -1, PlaylistName = "Tất cả bài hát (Thư viện)" });

                LogDebug("LoadUserPlaylists: Adding user playlists...");
                foreach (var pl in _playlists)
                {
                    PlaylistList.Items.Add(pl);
                    LogDebug($"  - Added playlist: {pl.PlaylistName}");
                }

                LogDebug("LoadUserPlaylists: Setting selected index to 0...");
                PlaylistList.SelectedIndex = 0;
                
                LogDebug("LoadUserPlaylists: Updating context menu...");
                UpdateAddToPlaylistMenu();
                
                // Clear playlist search box
                PlaylistSearchBox.Text = "";
                
                LogDebug("LoadUserPlaylists: COMPLETED");
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR in LoadUserPlaylists: {ex.Message}");
                throw;
            }
        }

        // ==================== LOAD LIBRARY (TẤT CẢ BÀI HÁT) ====================
        /// <summary>
        /// Tải TẤT CẢ bài hát từ database (không phân biệt playlist)
        /// Đây là view mặc định khi mở app hoặc khi click "Tất cả bài hát (Thư viện)"
        /// - Set _currentViewingPlaylist = null để biết đang xem Library
        /// - Load bài đầu tiên vào player (không auto play)
        /// - LƯU TẤT CẢ BÀI HÁT để dùng cho search filter
        /// </summary>
        private void LoadLibrarySongs()
        {
            try
            {
                LogDebug("LoadLibrarySongs: Getting all songs from service...");
                _songs = _musicService.GetAllSongs();
                _allSongsInCurrentView = new List<Song>(_songs); // NEW: Lưu để filter
                LogDebug($"LoadLibrarySongs: Got {_songs.Count} songs");

                LogDebug("LoadLibrarySongs: Setting SongList.ItemsSource...");
                SongList.ItemsSource = _songs;

                _currentViewingPlaylist = null;
                LogDebug("LoadLibrarySongs: Updating CurrentListTitle...");
                CurrentListTitle.Text = "Tất cả bài hát (Thư viện)";

                if (_songs.Count > 0 && _currentIndex == -1)
                {
                    LogDebug("LoadLibrarySongs: Setting current index to 0...");
                    _currentIndex = 0;
                    SongList.SelectedIndex = _currentIndex;
                    
                    LogDebug($"LoadLibrarySongs: Loading song info for: {_songs[_currentIndex].Title}");
                    LoadSongInfo(_songs[_currentIndex]);
                }

                LogDebug("LoadLibrarySongs: Updating UI states...");
                UpdateEmptyState();
                UpdateSongCount();
                
                // Clear song search box
                SongSearchBox.Text = "";
                
                LogDebug("LoadLibrarySongs: COMPLETED");
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR in LoadLibrarySongs: {ex.Message}");
                throw;
            }
        }

        // ==================== LOAD PLAYLIST CỤ THỂ ====================
        /// <summary>
        /// Tải bài hát từ 1 playlist cụ thể (không phải Library)
        /// - Lưu playlist đang xem vào _currentViewingPlaylist
        /// - Load danh sách bài hát trong playlist đó
        /// - Cập nhật title hiển thị tên playlist
        /// - LƯU TẤT CẢ BÀI HÁT để dùng cho search filter
        /// </summary>
        private void LoadSongsFromPlaylist(Playlist playlist)
        {
            _songs = _playlistService.GetSongsForPlaylist(playlist.PlaylistId);
            _allSongsInCurrentView = new List<Song>(_songs); // NEW: Lưu để filter
            SongList.ItemsSource = _songs;

            _currentViewingPlaylist = playlist;
            CurrentListTitle.Text = playlist.PlaylistName;

            if (_songs.Count > 0)
            {
                _currentIndex = 0;
                SongList.SelectedIndex = 0;
                LoadSongInfo(_songs[_currentIndex]);
            }
            else
            {
                _currentIndex = -1;
                ResetTimeDisplay();
            }

            UpdateEmptyState();
            UpdateSongCount();
            
            // Clear song search box
            SongSearchBox.Text = "";
        }

        // MỚI: Xử lý khi bấm chọn một playlist
        private void PlaylistList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaylistList.SelectedItem == null) return;
            var selected = (Playlist)PlaylistList.SelectedItem;

            if (selected.PlaylistId == -1)
            {
                LoadLibrarySongs();
            }
            else
            {
                LoadSongsFromPlaylist(selected);
            }
        }

        // MỚI: Xử lý nút "Tạo Playlist"
        private void CreatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            // Show input dialog to get playlist name
            var inputDialog = new InputDialog("Tạo Playlist Mới", "Nhập tên playlist:");
            if (inputDialog.ShowDialog() == true)
            {
                string name = inputDialog.InputText;
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Vui lòng nhập tên cho playlist mới.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    _playlistService.CreateNewPlaylist(_currentUser.UserId, name);
                    LoadUserPlaylists(); // Tải lại danh sách
                    MessageBox.Show($"Đã tạo playlist '{name}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tạo playlist: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void DeletePlaylistMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistList.SelectedItem == null) return;

            var selectedPlaylist = (Playlist)PlaylistList.SelectedItem;

            //Không cho xóa "Thư viện"
            if (selectedPlaylist.PlaylistId == -1)
            {
                MessageBox.Show("Bạn không thể xóa 'Tất cả bài hát (Thư viện)'.",
                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //Hỏi xác nhận
            var result = MessageBox.Show($"Bạn có chắc muốn xóa vĩnh viễn playlist: '{selectedPlaylist.PlaylistName}'?\n(Các bài hát sẽ không bị xóa khỏi thư viện.)",
                                         "Xác nhận xóa Playlist",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _playlistService.DeletePlaylist(selectedPlaylist.PlaylistId);
                    LoadUserPlaylists();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa playlist: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // MỚI: Cập nhật ContextMenu "Thêm vào Playlist"
        private void UpdateAddToPlaylistMenu()
        {
            AddToPlaylistMenu.Items.Clear();
            foreach (var pl in _playlists)
            {
                var menuItem = new MenuItem { Header = pl.PlaylistName, Tag = pl.PlaylistId };
                menuItem.Click += AddToPlaylistMenuItem_Click;
                AddToPlaylistMenu.Items.Add(menuItem);
            }
        }

        // MỚI: Xử lý khi bấm vào một playlist con trong ContextMenu
        private void AddToPlaylistMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (SongList.SelectedItem == null) return;

            var song = (Song)SongList.SelectedItem;
            var menuItem = (MenuItem)sender;
            int playlistId = (int)menuItem.Tag;

            _playlistService.AddSongToPlaylist(playlistId, song.SongId);
            MessageBox.Show($"Đã thêm '{song.Title}' vào playlist.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // MỚI: Xử lý khi bấm "Xóa khỏi Playlist này"
        private void RemoveFromPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (_currentViewingPlaylist == null)
            {
                MessageBox.Show("Bạn đang ở Thư viện. Chỉ có thể xóa bài hát khỏi một playlist cụ thể.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (SongList.SelectedItem == null) return;

            var song = (Song)SongList.SelectedItem;
            _playlistService.RemoveSongFromPlaylist(_currentViewingPlaylist.PlaylistId, song.SongId);
            LoadSongsFromPlaylist(_currentViewingPlaylist); // Tải lại list
        }

        // ==================== THÊM NHẠC TỪ LOCAL ====================
        /// <summary>
        /// Xử lý khi user click nút "Add Songs"
        /// QUAN TRỌNG để fix màn hình đen:
        /// 1. Validate từng file trước khi add (tồn tại, không rỗng, readable)
        /// 2. Disable UI + cursor Wait khi đang load
        /// 3. Load trong background thread (Task.Run)
        /// 4. Force UI refresh SAU KHI load xong
        /// 5. Hiển thị MessageBox khi UI đã render hoàn toàn
        /// </summary>
        private async void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("SelectFiles_Click: Opening file dialog...");
                var dlg = new OpenFileDialog { 
                    Title = "Chọn các file nhạc", 
                    Filter = "Tất cả file nhạc|*.mp3;*.wav;*.wma;*.aac;*.m4a;*.mp4;*.avi;*.wmv;*.mov|" +
                             "Audio Files|*.mp3;*.wav;*.wma;*.aac;*.m4a|" +
                             "Video Files|*.mp4;*.avi;*.wmv;*.mov|" +
                             "All Files|*.*",
                    Multiselect = true 
                };
                bool? result = dlg.ShowDialog();
                
                if (result != true) 
                {
                    LogDebug("SelectFiles_Click: User cancelled");
                    return;
                }

                var selectedFiles = dlg.FileNames.ToList();
                LogDebug($"SelectFiles_Click: User selected {selectedFiles.Count} files");

                // ========== BƯỚC 1: VALIDATE TỪNG FILE ==========
                // Kiểm tra file có hợp lệ không TRƯỚC KHI thêm vào database
                // Tránh add file lỗi gây crash khi play
                var invalidFiles = new List<string>();
                var validFiles = new List<string>();
                
                foreach (var file in selectedFiles)
                {
                    LogDebug($"Checking file: {file}");
                    
                    if (!File.Exists(file))
                    {
                        LogDebug($"  - File not found");
                        invalidFiles.Add($"{Path.GetFileName(file)} - File không tồn tại");
                        continue;
                    }
                    
                    try
                    {
                        // Check if file is readable and not corrupted
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.Length == 0)
                        {
                            LogDebug($"  - File is empty (0 bytes)");
                            invalidFiles.Add($"{Path.GetFileName(file)} - File rỗng (0 bytes)");
                            continue;
                        }
                        
                        // Try to open file to check if it's accessible
                        using (var stream = File.OpenRead(file))
                        {
                            // Read first byte to verify file is readable
                            stream.ReadByte();
                        }
                        
                        LogDebug($"  - File OK: {fileInfo.Length} bytes");
                        validFiles.Add(file);
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"  - File validation error: {ex.Message}");
                        invalidFiles.Add($"{Path.GetFileName(file)} - {ex.Message}");
                    }
                }

                // Show validation results
                if (invalidFiles.Count > 0)
                {
                    var message = $"Có {invalidFiles.Count} file không hợp lệ:\n\n" + 
                                  string.Join("\n", invalidFiles.Take(10));
                    if (invalidFiles.Count > 10)
                        message += $"\n\n... và {invalidFiles.Count - 10} file khác";
                    
                    if (validFiles.Count > 0)
                        message += $"\n\n{validFiles.Count} file hợp lệ sẽ được thêm vào.";
                    
                    MessageBox.Show(message, "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (validFiles.Count == 0)
                {
                    LogDebug("SelectFiles_Click: No valid files to add");
                    MessageBox.Show("Không có file nào hợp lệ để thêm vào!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // ========== BƯỚC 2: DISABLE UI ĐỂ TRÁNH FREEZE ==========
                // QUAN TRỌNG: Disable UI để user không click linh tinh khi đang load
                // Hiển thị cursor Wait để báo hiệu đang xử lý
                this.IsEnabled = false;
                this.Cursor = Cursors.Wait;

                List<Song> newSongs = null;
                try
                {
                    // ========== BƯỚC 3: LOAD TRONG BACKGROUND THREAD ==========
                    // Task.Run để không block UI thread -> tránh màn hình đen
                    LogDebug($"SelectFiles_Click: Loading {validFiles.Count} valid songs in background...");
                    newSongs = await Task.Run(() => _musicService.LoadLocalSongsFromFiles(validFiles));
                    LogDebug($"SelectFiles_Click: Loaded {newSongs?.Count ?? 0} new songs");
                }
                catch (Exception ex)
                {
                    LogDebug($"SelectFiles_Click ERROR: {ex.Message}");
                    
                    // Re-enable UI before showing error
                    this.IsEnabled = true;
                    this.Cursor = Cursors.Arrow;
                    this.UpdateLayout();
                    
                    MessageBox.Show($"Lỗi khi nhập file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ========== BƯỚC 4: RE-ENABLE UI VÀ FORCE REFRESH ==========
                // QUAN TRỌNG: Phải enable lại UI trước khi show MessageBox
                this.IsEnabled = true;
                this.Cursor = Cursors.Arrow;
                
                // ========== BƯỚC 5: FORCE UI REFRESH ĐỂ TRÁNH MÀN HÌNH ĐEN ==========
                // UpdateLayout() - Force WPF re-layout tất cả controls
                // InvalidateVisual() - Force WPF re-render visual tree
                // Dispatcher.InvokeAsync - Đảm bảo UI thread thực sự render xong
                LogDebug("SelectFiles_Click: Re-enabling UI and forcing refresh...");
                this.UpdateLayout();
                this.InvalidateVisual();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                LogDebug("SelectFiles_Click: UI re-enabled and refreshed");

                if (newSongs != null && newSongs.Count > 0)
                {
                    LogDebug("SelectFiles_Click: Reloading library songs...");
                    LoadLibrarySongs();
                    PlaylistList.SelectedIndex = 0;
                    
                    // Show message after UI is fully refreshed
                    await Dispatcher.InvokeAsync(() => {
                        MessageBox.Show($"Đã thêm {newSongs.Count} bài hát mới!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }, DispatcherPriority.ApplicationIdle);
                }
                else
                {
                    LogDebug("SelectFiles_Click: No new songs added (duplicates)");
                    
                    await Dispatcher.InvokeAsync(() => {
                        MessageBox.Show("Các bài nhạc đã tồn tại trong kho!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }, DispatcherPriority.ApplicationIdle);
                }
                
                LogDebug("SelectFiles_Click: Completed successfully");
            }
            catch (Exception ex)
            {
                LogDebug($"SelectFiles_Click FATAL ERROR: {ex.Message}");
                this.IsEnabled = true;
                this.Cursor = Cursors.Arrow;
                MessageBox.Show($"Lỗi nghiêm trọng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // SỬA: Dùng Service
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (SongList.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn bài hát để xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var song = (Song)SongList.SelectedItem;
            var result = MessageBox.Show($"Bạn có chắc muốn xóa vĩnh viễn bài: {song.Title} khỏi thư viện?\n(Hành động này sẽ xóa file khỏi TẤT CẢ playlist)", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (mediaPlayer.Source != null && mediaPlayer.Source.LocalPath == song.FilePath)
                {
                    mediaPlayer.Stop();
                    _timer?.Stop();
                }

                _musicService.DeleteSong(song.SongId);

                if (_currentViewingPlaylist == null)
                    LoadLibrarySongs();
                else
                    LoadSongsFromPlaylist(_currentViewingPlaylist);
            }
        }

        // =======================================================
        // CÁC HÀM PHÁT NHẠC (Giữ nguyên)
        // =======================================================

        private void Loop_Click(object sender, RoutedEventArgs e)
        {
            _isLoop = !_isLoop;
            if (LoopButton != null) LoopButton.Content = _isLoop ? "🔁  Loop ON" : "🔁  Loop";
        }

        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            _isShuffle = !_isShuffle;
            if (ShuffleButton != null) ShuffleButton.Content = _isShuffle ? "🔀  Shuffle ON" : "🔀  Shuffle";
        }

        private async void Play_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("Play_Click: Started");
                
                if (_currentIndex < 0 || _currentIndex >= _songs.Count)
                {
                    if (_songs.Count > 0)
                    {
                        _currentIndex = 0;
                        SongList.SelectedIndex = 0;
                        LogDebug("Play_Click: Reset index to 0");
                    }
                    else
                    {
                        LogDebug("Play_Click: No songs available");
                        MessageBox.Show("Không có bài hát nào để phát!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                
                if (_isPaused)
                {
                    LogDebug("Play_Click: Resuming from pause");
                    
                    // Force UI refresh before resuming
                    await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                    
                    mediaPlayer.Play();
                    _isPaused = false;
                    _timer?.Start();
                }
                else
                {
                    LogDebug($"Play_Click: Playing song at index {_currentIndex}");
                    PlaySong(_songs[_currentIndex]);
                }
                
                UpdatePlayPauseButtons();
                LogDebug("Play_Click: Completed");
            }
            catch (Exception ex)
            {
                LogDebug($"Play_Click ERROR: {ex.Message}");
                MessageBox.Show($"Lỗi khi phát nhạc: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Pause();
                _isPaused = true;
                _timer?.Stop();
            }
            UpdatePlayPauseButtons();
        }

        private void Next_Click(object? sender, RoutedEventArgs? e)
        {
            if (_songs.Count == 0) return;
            if (_isShuffle)
            {
                var rnd = new Random();
                _currentIndex = rnd.Next(_songs.Count);
            }
            else
            {
                _currentIndex = (_currentIndex + 1) % _songs.Count;
            }
            SongList.SelectedIndex = _currentIndex;
            PlaySong(_songs[_currentIndex]);
        }

        private void Prev_Click(object? sender, RoutedEventArgs? e)
        {
            if (_songs.Count == 0) return;
            _currentIndex = (_currentIndex - 1 + _songs.Count) % _songs.Count;
            SongList.SelectedIndex = _currentIndex;
            PlaySong(_songs[_currentIndex]);
        }

        private void SongList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SongList.SelectedIndex >= 0 && SongList.SelectedIndex < _songs.Count)
            {
                _currentIndex = SongList.SelectedIndex;
            }
        }

        // ==================== PHÁT NHẠC ====================
        /// <summary>
        /// Phát một bài hát cụ thể
        /// QUAN TRỌNG để fix màn hình đen khi play:
        /// 1. Stop và clear source cũ hoàn toàn
        /// 2. Render UI TRƯỚC với delay 50ms
        /// 3. Set source và delay 100ms cho MediaElement buffer
        /// 4. Play và restore cursor ngay
        /// Giải thích: MediaElement.Play() có thể block UI nếu file lớn hoặc codec phức tạp
        /// </summary>
        private async void PlaySong(Song song)
        {
            try
            {
                LogDebug($"PlaySong: Starting to play '{song.Title}'");
                
                // Kiểm tra file có tồn tại không
                if (!File.Exists(song.FilePath))
                {
                    LogDebug($"PlaySong ERROR: File not found - {song.FilePath}");
                    MessageBox.Show($"File không tồn tại:\n{song.FilePath}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ========== BƯỚC 1: STOP VÀ CLEAR HOÀN TOÀN ==========
                // Phải stop và clear source trước khi load bài mới
                // Tránh conflict giữa bài cũ và bài mới
                LogDebug("PlaySong: Stopping current media...");
                mediaPlayer.Stop();
                mediaPlayer.Source = null;
                _timer?.Stop();
                
                // ========== BƯỚC 2: UPDATE UI VỚI CURSOR WAIT ==========
                LogDebug("PlaySong: Updating UI for loading state...");
                this.Cursor = Cursors.Wait; // Báo hiệu đang load
                UpdateNowPlaying(song);      // Hiển thị tên bài đang load
                
                // ========== BƯỚC 3: FORCE UI RENDER TRƯỚC KHI LOAD MEDIA ==========
                // QUAN TRỌNG: Phải cho UI render xong TRƯỚC KHI load file nhạc
                // Nếu không, MediaElement sẽ block UI thread -> màn hình đen
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                await Task.Delay(50); // Delay 50ms để đảm bảo UI thực sự vẽ xong
                
                // ========== BƯỚC 4: SET SOURCE VÀ CHO BUFFER ==========
                // Set source nhưng CHƯA play ngay để MediaElement có time buffer
                LogDebug($"PlaySong: Setting media source to {song.FilePath}");
                mediaPlayer.Source = new Uri(song.FilePath);
                
                // Delay 100ms để MediaElement buffer một chút
                await Task.Delay(100);
                
                // ========== BƯỚC 5: PLAY VÀ RESTORE CURSOR ==========
                // Bây giờ mới play - MediaOpened event sẽ xử lý phần còn lại
                LogDebug("PlaySong: Calling Play()");
                mediaPlayer.Play();
                
                _isPaused = false;
                
                // Restore cursor
                this.Cursor = Cursors.Arrow;
                
                UpdatePlayPauseButtons();
                LogDebug("PlaySong: Completed successfully");
            }
            catch (Exception ex)
            {
                LogDebug($"PlaySong ERROR: {ex.Message}");
                this.Cursor = Cursors.Arrow;
                _timer?.Stop();
                MessageBox.Show($"Không thể phát bài hát:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadSongInfo(Song song)
        {
            try
            {
                LogDebug($"LoadSongInfo: Loading info for '{song.Title}'");
                
                if (!File.Exists(song.FilePath))
                {
                    LogDebug("LoadSongInfo: File not found");
                    ResetTimeDisplay();
                    return;
                }

                // Force UI refresh before loading media
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                
                await Dispatcher.InvokeAsync(() => {
                    mediaPlayer.Source = new Uri(song.FilePath);
                    mediaPlayer.Stop();
                    UpdateNowPlaying(song);
                }, DispatcherPriority.Normal);
                
                LogDebug("LoadSongInfo: Completed");
            }
            catch (Exception ex)
            {
                LogDebug($"LoadSongInfo ERROR: {ex.Message}");
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaPlayer != null) mediaPlayer.Volume = e.NewValue;
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaPlayer?.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan && !_timer.IsEnabled) // Chỉ cập nhật label khi kéo
            {
                var total = mediaPlayer.NaturalDuration.TimeSpan;
                var newPosition = TimeSpan.FromSeconds((ProgressSlider.Value / 100.0) * total.TotalSeconds);
                CurrentTimeLabel.Text = newPosition.ToString(@"mm\:ss");
            }
        }

        private void ProgressSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _timer?.Stop();
        }

        private void ProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mediaPlayer?.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                var total = mediaPlayer.NaturalDuration.TimeSpan;
                var newPosition = TimeSpan.FromSeconds((ProgressSlider.Value / 100.0) * total.TotalSeconds);
                mediaPlayer.Position = newPosition;
            }
            _timer?.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (mediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    var current = mediaPlayer.Position;
                    var total = mediaPlayer.NaturalDuration.TimeSpan;
                    if (total.TotalSeconds > 0)
                    {
                        ProgressSlider.Value = (current.TotalSeconds / total.TotalSeconds) * 100;
                        CurrentTimeLabel.Text = current.ToString(@"mm\:ss");
                        TotalTimeLabel.Text = total.ToString(@"mm\:ss");
                    }
                }
            }
            catch { }
        }

        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                var total = mediaPlayer.NaturalDuration.TimeSpan;
                TotalTimeLabel.Text = total.ToString(@"mm\:ss");
                _timer?.Start();
            }
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (_isLoop)
            {
                PlaySong(_songs[_currentIndex]);
            }
            else
            {
                Next_Click(null, null);
            }
        }

        private void ResetTimeDisplay()
        {
            CurrentTimeLabel.Text = "0:00";
            TotalTimeLabel.Text = "0:00";
            ProgressSlider.Value = 0;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) 
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else 
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
            Application.Current.Shutdown();
        }

        private void UpdateEmptyState()
        {
            if (EmptyState != null) EmptyState.Visibility = _songs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateSongCount()
        {
            if (SongCountLabel != null) SongCountLabel.Text = _songs.Count == 1 ? "1 song" : $"{_songs.Count} songs";
        }

        private void UpdateNowPlaying(Song song)
        {
            if (NowPlayingLabel != null) NowPlayingLabel.Text = song.Title;
        }

        private void UpdatePlayPauseButtons()
        {
            if (PlayButton != null && PauseButton != null)
            {
                if (_isPaused || mediaPlayer.Source == null)
                {
                    PlayButton.Visibility = Visibility.Visible;
                    PauseButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    PlayButton.Visibility = Visibility.Collapsed;
                    PauseButton.Visibility = Visibility.Visible;
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _timer?.Stop();
            mediaPlayer?.Stop();
        }

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
        }

        // ==================== WINDOW LOADED EVENT ====================
        /// <summary>
        /// Event chạy sau khi Window đã được load hoàn toàn
        /// QUAN TRỌNG: Force UI refresh lần cuối để fix màn hình đen
        /// Đây là safety net cuối cùng đảm bảo UI được render
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("===== Window_Loaded EVENT =====");
                // Log thông tin window để debug
                LogDebug($"Window ActualWidth: {this.ActualWidth}");
                LogDebug($"Window ActualHeight: {this.ActualHeight}");
                LogDebug($"Window IsVisible: {this.IsVisible}");
                LogDebug($"Window IsLoaded: {this.IsLoaded}");
                LogDebug($"WindowState: {this.WindowState}");
                LogDebug($"Background: {this.Background}");
                
                // ========== FORCE UI REFRESH LẦN CUỐI ==========
                // Đây là lần cuối cùng đảm bảo UI được vẽ đúng
                // Fix trường hợp màn hình đen do WPF rendering issue
                LogDebug("🔧 Forcing UI update...");
                this.UpdateLayout();      // Force layout
                this.InvalidateVisual();  // Force visual render
                Dispatcher.Invoke(() => { }, DispatcherPriority.Render); // Force dispatcher render
                LogDebug("✅ UI update completed");
                
                LogDebug("===== Window_Loaded COMPLETED =====");
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR in Window_Loaded: {ex.Message}");
            }
        }

        // ==================== SEARCH FUNCTIONALITY ====================

        /// <summary>
        /// Xử lý tìm kiếm bài hát theo tên
        /// - Tìm kiếm trong danh sách hiện tại (Library hoặc Playlist cụ thể)
        /// - Không phân biệt hoa thường
        /// - Tìm theo từ khóa có trong tên bài hát
        /// - Giữ nguyên _allSongsInCurrentView để có thể clear search và quay lại
        /// </summary>
        private void SongSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var keyword = SongSearchBox.Text.Trim();
                LogDebug($"SongSearchBox_TextChanged: Searching for '{keyword}'");

                // Toggle placeholder visibility
                SongSearchPlaceholder.Visibility = string.IsNullOrEmpty(keyword) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;

                // If search is empty, show all songs
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    LogDebug("SongSearchBox_TextChanged: Empty search, showing all songs");
                    _songs = new List<Song>(_allSongsInCurrentView);
                    SongList.ItemsSource = _songs;
                    UpdateEmptyState();
                    UpdateSongCount();
                    return;
                }

                // Filter songs by keyword (case-insensitive)
                var filteredSongs = _allSongsInCurrentView
                    .Where(s => s.Title != null && 
                               s.Title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                LogDebug($"SongSearchBox_TextChanged: Found {filteredSongs.Count} matching songs");

                // Update UI with filtered results
                _songs = filteredSongs;
                SongList.ItemsSource = _songs;

                // Reset selection if no songs found
                if (_songs.Count == 0)
                {
                    _currentIndex = -1;
                    ResetTimeDisplay();
                }
                else
                {
                    // Select first song in filtered list
                    _currentIndex = 0;
                    SongList.SelectedIndex = 0;
                }

                UpdateEmptyState();
                UpdateSongCount();
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR in SongSearchBox_TextChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// Xử lý tìm kiếm playlist theo tên
        /// - Tìm kiếm trong tất cả playlist của user
        /// - Luôn giữ "Tất cả bài hát (Thư viện)" ở đầu danh sách
        /// - Không phân biệt hoa thường
        /// - Tìm theo từ khóa có trong tên playlist
        /// </summary>
        private void PlaylistSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var keyword = PlaylistSearchBox.Text.Trim();
                LogDebug($"PlaylistSearchBox_TextChanged: Searching for '{keyword}'");

                // Toggle placeholder visibility
                PlaylistSearchPlaceholder.Visibility = string.IsNullOrEmpty(keyword) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;

                // Clear current playlist list
                PlaylistList.ItemsSource = null;
                PlaylistList.Items.Clear();

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    // Show all playlists if search is empty
                    LogDebug("PlaylistSearchBox_TextChanged: Empty search, showing all playlists");
                    
                    foreach (var pl in _allPlaylists)
                    {
                        PlaylistList.Items.Add(pl);
                    }
                }
                else
                {
                    // Filter playlists by keyword (case-insensitive)
                    // Always keep "Tất cả bài hát (Thư viện)" at top
                    var library = _allPlaylists.FirstOrDefault(p => p.PlaylistId == -1);
                    if (library != null)
                    {
                        PlaylistList.Items.Add(library);
                    }

                    var filteredPlaylists = _allPlaylists
                        .Where(p => p.PlaylistId != -1 && // Skip library (already added)
                                   p.PlaylistName != null && 
                                   p.PlaylistName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();

                    LogDebug($"PlaylistSearchBox_TextChanged: Found {filteredPlaylists.Count} matching playlists");

                    foreach (var pl in filteredPlaylists)
                    {
                        PlaylistList.Items.Add(pl);
                    }
                }

                // Keep current selection if possible
                if (PlaylistList.Items.Count > 0)
                {
                    // Try to find current viewing playlist in filtered list
                    var currentPlaylistInList = PlaylistList.Items.Cast<Playlist>()
                        .FirstOrDefault(p => _currentViewingPlaylist == null 
                            ? p.PlaylistId == -1 
                            : p.PlaylistId == _currentViewingPlaylist.PlaylistId);

                    if (currentPlaylistInList != null)
                    {
                        PlaylistList.SelectedItem = currentPlaylistInList;
                    }
                    else
                    {
                        PlaylistList.SelectedIndex = 0;
                    }
                }

                UpdateAddToPlaylistMenu(); // Update context menu với playlists đã filter
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR in PlaylistSearchBox_TextChanged: {ex.Message}");
            }
        }
    }
}