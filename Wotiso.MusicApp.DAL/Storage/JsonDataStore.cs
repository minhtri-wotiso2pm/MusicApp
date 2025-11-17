using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wotiso.MusicApp.DAL.Entities;

namespace Wotiso.MusicApp.DAL.Storage
{
    /// <summary>
    /// JSON-based data storage - replaces SQLite database
    /// L?u t?t c? d? li?u vào file JSON thay vì database
    /// Portable và d? backup/restore
    /// </summary>
    public class JsonDataStore
    {
        private readonly string _dataFilePath;
        private MusicAppData _data;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public JsonDataStore(string dataFilePath = null)
        {
            // L?u trong th? m?c workspace: D:\FPT\FA25\PRN212\Project3\MusicApp\Data\musicapp_data.json
            _dataFilePath = dataFilePath ?? Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..",  // Lùi 4 c?p t? bin\Debug\net9.0 v? workspace root
                "Data",
                "musicapp_data.json"
            );

            // Normalize path ?? lo?i b? ".."
            _dataFilePath = Path.GetFullPath(_dataFilePath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(_dataFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            LoadData();
        }

        /// <summary>
        /// Load data from JSON file
        /// </summary>
        private void LoadData()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    var json = File.ReadAllText(_dataFilePath);
                    _data = JsonSerializer.Deserialize<MusicAppData>(json, _jsonOptions) ?? new MusicAppData();
                }
                else
                {
                    _data = new MusicAppData();
                    SaveData(); // Create initial file
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}. Creating new data file.");
                _data = new MusicAppData();
                SaveData();
            }
        }

        /// <summary>
        /// Save data to JSON file
        /// </summary>
        public void SaveData()
        {
            try
            {
                var json = JsonSerializer.Serialize(_data, _jsonOptions);
                File.WriteAllText(_dataFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data: {ex.Message}");
                throw;
            }
        }

        // ==================== SONGS ====================
        public List<Song> GetAllSongs() => _data.Songs;

        public Song GetSongById(int id) => _data.Songs.Find(s => s.SongId == id);

        public void AddSong(Song song)
        {
            song.SongId = _data.NextSongId++;
            song.CreatedAt = DateTime.Now;
            _data.Songs.Add(song);
            SaveData();
        }

        public void BulkAddSongs(List<Song> songs)
        {
            foreach (var song in songs)
            {
                song.SongId = _data.NextSongId++;
                song.CreatedAt = DateTime.Now;
                _data.Songs.Add(song);
            }
            SaveData();
        }

        public void UpdateSong(Song song)
        {
            var index = _data.Songs.FindIndex(s => s.SongId == song.SongId);
            if (index >= 0)
            {
                _data.Songs[index] = song;
                SaveData();
            }
        }

        public void DeleteSong(int id)
        {
            // Remove from songs list
            _data.Songs.RemoveAll(s => s.SongId == id);

            // Remove from all playlists
            foreach (var playlist in _data.Playlists)
            {
                playlist.SongIds.Remove(id);
            }

            SaveData();
        }

        public List<Song> GetFavoriteSongs()
        {
            return _data.Songs.FindAll(s => s.IsFavorite);
        }

        // ==================== PLAYLISTS ====================
        public List<Playlist> GetAllPlaylists() => _data.Playlists;

        public Playlist GetPlaylistById(int id) => _data.Playlists.Find(p => p.PlaylistId == id);

        public Playlist CreatePlaylist(Playlist playlist)
        {
            playlist.PlaylistId = _data.NextPlaylistId++;
            playlist.CreatedAt = DateTime.Now;
            playlist.SongIds = new List<int>();
            _data.Playlists.Add(playlist);
            SaveData();
            return playlist;
        }

        public void DeletePlaylist(int id)
        {
            _data.Playlists.RemoveAll(p => p.PlaylistId == id);
            SaveData();
        }

        public void AddSongToPlaylist(int playlistId, int songId)
        {
            var playlist = GetPlaylistById(playlistId);
            if (playlist != null && !playlist.SongIds.Contains(songId))
            {
                playlist.SongIds.Add(songId);
                SaveData();
            }
        }

        public void RemoveSongFromPlaylist(int playlistId, int songId)
        {
            var playlist = GetPlaylistById(playlistId);
            if (playlist != null)
            {
                playlist.SongIds.Remove(songId);
                SaveData();
            }
        }

        public List<Song> GetSongsForPlaylist(int playlistId)
        {
            var playlist = GetPlaylistById(playlistId);
            if (playlist == null) return new List<Song>();

            var songs = new List<Song>();
            foreach (var songId in playlist.SongIds)
            {
                var song = GetSongById(songId);
                if (song != null)
                {
                    songs.Add(song);
                }
            }
            return songs;
        }
    }

    /// <summary>
    /// Data model for JSON storage
    /// </summary>
    public class MusicAppData
    {
        public List<Song> Songs { get; set; } = new();
        public List<Playlist> Playlists { get; set; } = new();
        public int NextSongId { get; set; } = 1;
        public int NextPlaylistId { get; set; } = 1;
    }
}
