using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Wotiso.MusicApp.DAL.Entities;

namespace Wotiso.MusicApp.DAL.Repositories
{
    public class SongRepository
    {
        private readonly MusicPlayerDbContext _context;

        public SongRepository(MusicPlayerDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả bài hát
        public List<Song> GetAll() => _context.Songs.ToList();

        // Lấy bài hát theo ID
        public Song? GetById(int id) => _context.Songs.FirstOrDefault(s => s.SongId == id);

        // Thêm bài hát vào DB (single)
        public void Add(Song song)
        {
            _context.Songs.Add(song);
            _context.SaveChanges();
        }

        // Thêm nhiều bài hát (bulk) — gọi SaveChanges once for performance
        public void BulkAdd(IEnumerable<Song> songs)
        {
            if (songs == null) return;
            _context.Songs.AddRange(songs);
            _context.SaveChanges();
        }

        // Cập nhật bài hát (update)
        public void Update(Song song)
        {
            if (song == null) return;
            _context.Songs.Update(song);
            _context.SaveChanges();
        }

        // Kiểm tra tồn tại theo file path
        public bool ExistsByPath(string path)
        {
            return _context.Songs.Any(s => s.FilePath == path);
        }

        // Lấy các bài yêu thích (global flag)
        public List<Song> GetFavorites() => _context.Songs.Where(s => s.IsFavorite).ToList();

        // Xóa bài hát theo ID (safe: remove dependents first)
        public void Delete(int id)
        {
            var song = _context.Songs.Find(id);
            if (song == null) return;

            // Start a transaction to ensure atomicity
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Remove dependent DownloadHistory entries that reference this song
                var downloads = _context.DownloadHistories.Where(d => d.SongId == id).ToList();
                if (downloads.Count > 0)
                {
                    _context.DownloadHistories.RemoveRange(downloads);
                }

                // Remove dependent PlaylistSong entries (junction table)
                var playlistLinks = _context.PlaylistSongs.Where(ps => ps.SongId == id).ToList();
                if (playlistLinks.Count > 0)
                {
                    _context.PlaylistSongs.RemoveRange(playlistLinks);
                }

                // Remove many-to-many Favorite entries in the shadow "Favorites" table (if any)
                // This uses a direct SQL delete for the shadow join table.
                _context.Database.ExecuteSqlRaw("DELETE FROM Favorites WHERE SongId = {0}", id);

                // Now safe to remove the song
                _context.Songs.Remove(song);

                _context.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // Thêm lịch sử download
        public void AddDownloadHistory(DownloadHistory history)
        {
            _context.DownloadHistories.Add(history);
            _context.SaveChanges();
        }
    }
}
