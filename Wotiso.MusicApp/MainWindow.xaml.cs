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
using Wotiso.MusicApp.DAL;
using Wotiso.MusicApp.DAL.Entities;
using Wotiso.MusicApp.DAL.Repositories;


namespace Wotiso.MusicApp
{
    public partial class MainWindow : Window
    {
        private MusicService _musicService;
        private List<Song> _songs = new();
        private int _currentIndex = -1;
        private DispatcherTimer _timer;
        private bool _isPaused = false;
        private bool _isLoop = false;
        private bool _isShuffle = false;

        public MainWindow()
        {
            InitializeComponent();

            var context = new MusicPlayerDbContext();
            var repo = new SongRepository(context);
            _musicService = new MusicService(repo);

            LoadSongsFromDB();

            // Timer cập nhật progress
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += Timer_Tick;

            VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
            mediaPlayer.Volume = VolumeSlider.Value;

            // Set up progress slider interaction
            ProgressSlider.ValueChanged += ProgressSlider_ValueChanged;

            UpdateEmptyState();
        }

        private void LoadSongsFromDB()
        {
            _songs = _musicService.GetAllSongs();
            SongList.ItemsSource = _songs;

            if (_songs.Count > 0)
            {
                _currentIndex = 0;
                SongList.SelectedIndex = _currentIndex;
                LoadSongInfo(_songs[_currentIndex]);
            }

            UpdateEmptyState();
            UpdateSongCount();
        }

        // Make async so UI isn't blocked when saving many files
        private async void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Chọn các file nhạc",
                Filter = "Nhạc|*.mp3;*.wav;*.mp4",
                Multiselect = true
            };

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                var selectedFiles = dlg.FileNames.ToList();

                List<Song> newSongs = null;
                try
                {
                    // Run import on threadpool: MusicService will call BulkAdd
                    newSongs = await Task.Run(() => _musicService.LoadLocalSongsFromFiles(selectedFiles));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi nhập file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (newSongs != null && newSongs.Count > 0)
                {
                    // Refresh in UI thread
                    _songs.AddRange(newSongs);
                    SongList.ItemsSource = null;
                    SongList.ItemsSource = _songs;

                    if (_currentIndex == -1)
                    {
                        _currentIndex = 0;
                        SongList.SelectedIndex = _currentIndex;
                    }

                    UpdateEmptyState();
                    UpdateSongCount();

                    MessageBox.Show($"Đã thêm {newSongs.Count} bài hát mới!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Các bài nhạc đã tồn tại trong kho!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex >= 0 && _currentIndex < _songs.Count)
            {
                var song = _songs[_currentIndex];
                var result = MessageBox.Show($"Bạn có chắc muốn xóa bài: {song.Title}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _musicService.DeleteSong(song.SongId);
                    // If currently playing this file, stop playback
                    if (mediaPlayer.Source != null && mediaPlayer.Source.LocalPath == song.FilePath)
                    {
                        mediaPlayer.Stop();
                        _timer?.Stop();
                    }

                    _songs.RemoveAt(_currentIndex);

                    SongList.ItemsSource = null;
                    SongList.ItemsSource = _songs;

                    if (_songs.Count > 0)
                    {
                        _currentIndex = Math.Min(_currentIndex, _songs.Count - 1);
                        SongList.SelectedIndex = _currentIndex;
                    }
                    else
                    {
                        _currentIndex = -1;
                        ResetTimeDisplay();
                    }

                    UpdateEmptyState();
                    UpdateSongCount();
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn bài hát để xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Loop_Click(object sender, RoutedEventArgs e)
        {
            _isLoop = !_isLoop;
            if (LoopButton != null)
            {
                LoopButton.Content = _isLoop ? "🔁  Loop ON" : "🔁  Loop";
            }
        }

        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            _isShuffle = !_isShuffle;
            if (ShuffleButton != null)
            {
                ShuffleButton.Content = _isShuffle ? "🔀  Shuffle ON" : "🔀  Shuffle";
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < 0 || _currentIndex >= _songs.Count)
            {
                MessageBox.Show("Vui lòng chọn bài hát để phát!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
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

        private void Next_Click(object sender, RoutedEventArgs e)
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

        private void Prev_Click(object sender, RoutedEventArgs e)
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
                LoadSongInfo(_songs[_currentIndex]);
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
                // timer will start on MediaOpened once NaturalDuration available
                // but start now to update position if available
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
                // Preload source to get metadata on MediaOpened
                mediaPlayer.Source = new Uri(song.FilePath);
                mediaPlayer.Stop();

                UpdateNowPlaying(song);
            }
            catch { }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaPlayer != null) mediaPlayer.Volume = VolumeSlider.Value;
        }

        private bool _isUpdatingProgress = false;
        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingProgress) return;

            if (mediaPlayer?.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                var total = mediaPlayer.NaturalDuration.TimeSpan;
                var newPosition = TimeSpan.FromSeconds((ProgressSlider.Value / 100.0) * total.TotalSeconds);
                mediaPlayer.Position = newPosition;
            }
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
                        _isUpdatingProgress = true;
                        ProgressSlider.Value = (current.TotalSeconds / total.TotalSeconds) * 100;
                        _isUpdatingProgress = false;

                        CurrentTimeLabel.Text = current.ToString(@"mm\:ss");
                        TotalTimeLabel.Text = total.ToString(@"mm\:ss");
                    }
                }
            }
            catch
            {
                // ignore occasional media access exceptions
            }
        }

        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                var total = mediaPlayer.NaturalDuration.TimeSpan;
                TotalTimeLabel.Text = total.ToString(@"mm\:ss");
                // start timer to update progress
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
            _isUpdatingProgress = true;
            ProgressSlider.Value = 0;
            _isUpdatingProgress = false;
        }

        // Window control handlers
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

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // UI Helper Methods
        private void UpdateEmptyState()
        {
            if (EmptyState != null)
            {
                EmptyState.Visibility = _songs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateSongCount()
        {
            if (SongCountLabel != null)
            {
                SongCountLabel.Text = _songs.Count == 1 ? "1 song" : $"{_songs.Count} songs";
            }
        }

        private void UpdateNowPlaying(Song song)
        {
            if (NowPlayingLabel != null)
            {
                NowPlayingLabel.Text = song.Title;
            }
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
            // MediaElement has no Close() method; stop is enough
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