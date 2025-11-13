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

        // DEBUG: Logger
        private void LogDebug(string message)
        {
            Debug.WriteLine($"[MainWindow] {DateTime.Now:HH:mm:ss.fff} - {message}");
            try
            {
                File.AppendAllText("D:\\musicapp_debug.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}\n");
            }
            catch { }
        }

        // SỬA: Constructor mới nhận 3 tham số
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

        // MỚI: Tải danh sách playlist của user
        private void LoadUserPlaylists()
        {
            try
            {
                LogDebug("LoadUserPlaylists: Getting playlists from service...");
                _playlists = _playlistService.GetPlaylistsForUser(_currentUser.UserId);
                LogDebug($"LoadUserPlaylists: Got {_playlists.Count} playlists");

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
                
                LogDebug("LoadUserPlaylists: COMPLETED");
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR in LoadUserPlaylists: {ex.Message}");
                throw;
            }
        }

        // SỬA: Dùng service để tải thư viện chung
        private void LoadLibrarySongs()
        {
            try
            {
                LogDebug("LoadLibrarySongs: Getting all songs from service...");
                _songs = _musicService.GetAllSongs();
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
                
                LogDebug("LoadLibrarySongs: COMPLETED");
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR in LoadLibrarySongs: {ex.Message}");
                throw;
            }
        }

        // MỚI: Tải bài hát từ một playlist cụ thể
        private void LoadSongsFromPlaylist(Playlist playlist)
        {
            _songs = _playlistService.GetSongsForPlaylist(playlist.PlaylistId);
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
            string name = NewPlaylistNameBox.Text;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên cho playlist mới.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _playlistService.CreateNewPlaylist(_currentUser.UserId, name);
                NewPlaylistNameBox.Text = "";
                LoadUserPlaylists(); // Tải lại danh sách
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo playlist: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // SỬA: Dùng Service
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

                // Validate files first
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

                // Disable UI temporarily to prevent user actions
                this.IsEnabled = false;
                this.Cursor = Cursors.Wait;

                List<Song> newSongs = null;
                try
                {
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

                // Re-enable UI
                this.IsEnabled = true;
                this.Cursor = Cursors.Arrow;
                
                // Force UI refresh
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

        private async void PlaySong(Song song)
        {
            try
            {
                LogDebug($"PlaySong: Starting to play '{song.Title}'");
                
                if (!File.Exists(song.FilePath))
                {
                    LogDebug($"PlaySong ERROR: File not found - {song.FilePath}");
                    MessageBox.Show($"File không tồn tại:\n{song.FilePath}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Stop current playback first
                LogDebug("PlaySong: Stopping current media...");
                mediaPlayer.Stop();
                mediaPlayer.Source = null;
                _timer?.Stop();
                
                // Update UI immediately to show we're loading
                LogDebug("PlaySong: Updating UI for loading state...");
                this.Cursor = Cursors.Wait;
                UpdateNowPlaying(song);
                
                // Force UI refresh FIRST
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                await Task.Delay(50); // Give UI time to actually render
                
                // Set source WITHOUT playing immediately - let MediaOpened handle play
                LogDebug($"PlaySong: Setting media source to {song.FilePath}");
                mediaPlayer.Source = new Uri(song.FilePath);
                
                // Small delay to let MediaElement start loading
                await Task.Delay(100);
                
                // Now play - MediaOpened event will handle the rest
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
            if (e.ClickCount == 2) WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            else DragMove();
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("===== Window_Loaded EVENT =====");
                LogDebug($"Window ActualWidth: {this.ActualWidth}");
                LogDebug($"Window ActualHeight: {this.ActualHeight}");
                LogDebug($"Window IsVisible: {this.IsVisible}");
                LogDebug($"Window IsLoaded: {this.IsLoaded}");
                LogDebug($"WindowState: {this.WindowState}");
                LogDebug($"Background: {this.Background}");
                
                // 🔧 FORCE UI REFRESH - Fix black screen
                LogDebug("🔧 Forcing UI update...");
                this.UpdateLayout();
                this.InvalidateVisual();
                Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
                LogDebug("✅ UI update completed");
                
                LogDebug("===== Window_Loaded COMPLETED =====");
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR in Window_Loaded: {ex.Message}");
            }
        }
    }
}