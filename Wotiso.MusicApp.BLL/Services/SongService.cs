using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wotiso.MusicApp.DAL.Entities;
using Wotiso.MusicApp.DAL.Repositories;

namespace Wotiso.MusicApp.BLL.Services
{
    public class SongService
    {
        private readonly SongRepository _repo;

        public SongService(SongRepository repo)
        {
            _repo = repo;
        }
                    
        public List<Song> GetAllSongs() => _repo.GetAll();

        public List<Song> GetFavoriteSongs() => _repo.GetFavorites();

        // Efficiently load multiple files and persist them in one SaveChanges call
        public List<Song> LoadLocalSongsFromFiles(List<string> files)
        {
            var newSongs = new List<Song>();
            if (files == null || files.Count == 0) return newSongs;

            // Read existing file paths once for performance
            var existingPaths = new HashSet<string>(_repo.GetAll().Select(s => s.FilePath), StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file)) continue;
                if (!File.Exists(file)) continue;

                if (existingPaths.Contains(file)) continue;

                var song = new Song
                {
                    Title = Path.GetFileNameWithoutExtension(file),
                    FilePath = file,
                    FileType = Path.GetExtension(file),
                    CreatedAt = DateTime.Now,
                    IsFavorite = false
                };

                newSongs.Add(song);
                existingPaths.Add(file);
            }

            if (newSongs.Count > 0)
            {
                _repo.BulkAdd(newSongs);
            }

            return newSongs;
        }

        public void DeleteSong(int id) => _repo.Delete(id);

        // Thêm/xóa favorite sử dụng cờ IsFavorite (global)
        public void AddToFavorites(Song song)
        {
            if (song == null) return;
            song.IsFavorite = true;
            _repo.Update(song);
        }

        public void RemoveFromFavorites(Song song)
        {
            if (song == null) return;
            song.IsFavorite = false;
            _repo.Update(song);
        }

        // Ghi lịch sử download
        public void LogDownload(Song song)
        {
            if (song == null) return;

            try
            {
                var history = new DownloadHistory
                {
                    SongId = song.SongId,
                    UserId = 1, // user mặc định - consider replacing with real user later
                    DownloadedAt = DateTime.Now
                };
                _repo.AddDownloadHistory(history);
            }
            catch
            {
                // ignore to avoid crash if DB unavailable
            }
        }

        // Reorder songs is UI-level behavior in this version; persisting order requires playlist model.
        public void ReorderSongs(List<Song> newOrder)
        {
            // Keep in-memory only. Persisting requires playlist/order model and repository support.
            // UI should replace its collection with newOrder.
            // Intentionally left as no-DB operation.
        }

        public List<Song> handeFindyByKeyword(String keyword)
        {
            return _repo.HandleFindByKeyword(keyword);
        }
    }
}
