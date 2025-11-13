using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

        // SỬA: Constructor mới nhận 3 tham số
        public MainWindow(User loggedInUser, MusicService musicService, PlaylistService playlistService)
        {
            InitializeComponent();

            _currentUser = loggedInUser;
            _musicService = musicService;
            _playlistService = playlistService;

            // Tải dữ liệu của user
            LoadUserPlaylists();
            LoadLibrarySongs(); // Tải thư viện (tất cả bài hát) làm mặc định

            // Timer cập nhật progress
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += Timer_Tick;

            VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
            mediaPlayer.Volume = VolumeSlider.Value;

            // Sửa lỗi thanh tua nhạc
            ProgressSlider.ValueChanged += ProgressSlider_ValueChanged;
            ProgressSlider.PreviewMouseDown += ProgressSlider_PreviewMouseDown;
            ProgressSlider.PreviewMouseUp += ProgressSlider_PreviewMouseUp;

            UpdateEmptyState();
        }

        // MỚI: Tải danh sách playlist của user
        private void LoadUserPlaylists()
        {
            _playlists = _playlistService.GetPlaylistsForUser(_currentUser.UserId);

            PlaylistList.ItemsSource = null;
            PlaylistList.Items.Clear();
            PlaylistList.Items.Add(new Playlist { PlaylistId = -1, PlaylistName = "Tất cả bài hát (Thư viện)" });

            foreach (var pl in _playlists)
            {
                PlaylistList.Items.Add(pl);
            }

            PlaylistList.SelectedIndex = 0;
            UpdateAddToPlaylistMenu();
        }

        // SỬA: Dùng service để tải thư viện chung
        private void LoadLibrarySongs()
        {
            _songs = _musicService.GetAllSongs();
            SongList.ItemsSource = _songs;

            _currentViewingPlaylist = null;
            CurrentListTitle.Text = "Tất cả bài hát (Thư viện)";

            if (_songs.Count > 0 && _currentIndex == -1)
            {
                _currentIndex = 0;
                SongList.SelectedIndex = _currentIndex;
                LoadSongInfo(_songs[_currentIndex]);
            }

            UpdateEmptyState();
            UpdateSongCount();
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
            var dlg = new OpenFileDialog { Title = "Chọn các file nhạc", Filter = "Nhạc|*.mp3;*.wav;*.mp4", Multiselect = true };
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                var selectedFiles = dlg.FileNames.ToList();
                List<Song> newSongs = null;
                try
                {
                    newSongs = await Task.Run(() => _musicService.LoadLocalSongsFromFiles(selectedFiles));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi nhập file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (newSongs != null && newSongs.Count > 0)
                {
                    LoadLibrarySongs();
                    PlaylistList.SelectedIndex = 0;
                    MessageBox.Show($"Đã thêm {newSongs.Count} bài hát mới!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Các bài nhạc đã tồn tại trong kho!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
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

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < 0 || _currentIndex >= _songs.Count)
            {
                if (_songs.Count > 0)
                {
                    _currentIndex = 0;
                    SongList.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show("Không có bài hát nào để phát!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            if (_isPaused)
            {
                mediaPlayer.Play();
                _isPaused = false;
                _timer?.Start();
            }
            else
            {
                PlaySong(_songs[_currentIndex]);
            }
            UpdatePlayPauseButtons();
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

        private void PlaySong(Song song)
        {
            if (!File.Exists(song.FilePath))
            {
                MessageBox.Show($"File không tồn tại:\n{song.FilePath}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                mediaPlayer.Source = new Uri(song.FilePath);
                mediaPlayer.Play();
                _isPaused = false;
                _timer?.Start();
                UpdateNowPlaying(song);
                UpdatePlayPauseButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể phát bài hát:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSongInfo(Song song)
        {
            if (!File.Exists(song.FilePath))
            {
                ResetTimeDisplay();
                return;
            }
            try
            {
                mediaPlayer.Source = new Uri(song.FilePath);
                mediaPlayer.Stop();
                UpdateNowPlaying(song);
            }
            catch { }
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
    }
}