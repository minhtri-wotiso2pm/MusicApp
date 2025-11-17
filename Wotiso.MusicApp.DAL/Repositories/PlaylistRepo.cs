using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Wotiso.MusicApp.DAL.Entities;

namespace Wotiso.MusicApp.DAL.Repositories
{
    public class PlaylistRepo
    {
        private readonly MusicPlayerDbContext _context;

        public PlaylistRepo(MusicPlayerDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả playlist (không phân biệt user)
        public List<Playlist> GetAllPlaylists()
        {
            return _context.Playlists
                .AsNoTracking()
                .ToList();
        }

        // DEPRECATED: Giữ lại để backward compatible
        public List<Playlist> GetPlaylistsByUserId(int userId)
        {
            return GetAllPlaylists(); // Trả về tất cả playlist
        }

        // Tạo playlist mới
        public Playlist CreatePlaylist(Playlist playlist)
        {
            _context.Playlists.Add(playlist);
            _context.SaveChanges();
            return playlist;
        }

        //Xóa playlist
        public void DeletePlaylist(int playlistId)
        {
            var playlist = _context.Playlists
                                .Include(p => p.PlaylistSongs) // Lấy cả các bài hát liên quan
                                .FirstOrDefault(p => p.PlaylistId == playlistId);

            if (playlist != null)
            {
                // Xóa các liên kết trong bảng PlaylistSong
                _context.PlaylistSongs.RemoveRange(playlist.PlaylistSongs);
                _context.Playlists.Remove(playlist);
                _context.SaveChanges();
            }
        }

        // Thêm bài hát vào playlist
        public void AddSongToPlaylist(int playlistId, int songId)
        {
            //var exists = _context.PlaylistSongs
            //    .Any(ps => ps.PlaylistId == playlistId && ps.SongId == songId);

            //if (exists) return;

            int maxOrder = _context.PlaylistSongs
                .Where(ps => ps.PlaylistId == playlistId)
                .Select(ps => (int?)ps.OrderIndex)
                .Max() ?? -1;

            var newLink = new PlaylistSong
            {
                PlaylistId = playlistId,
                SongId = songId,
                OrderIndex = maxOrder + 1
            };

            _context.PlaylistSongs.Add(newLink);
            _context.SaveChanges();
        }

        // Xóa bài hát khỏi playlist
        public void RemoveSongFromPlaylist(int playlistId, int songId)
        {
            var link = _context.PlaylistSongs
                .FirstOrDefault(ps => ps.PlaylistId == playlistId && ps.SongId == songId);

            if (link != null)
            {
                _context.PlaylistSongs.Remove(link);
                _context.SaveChanges();
            }
        }

        // Lấy danh sách bài hát của 1 playlist
        public List<Song> GetSongsForPlaylist(int playlistId)
        {
            return _context.PlaylistSongs
                .Where(ps => ps.PlaylistId == playlistId)
                .OrderBy(ps => ps.OrderIndex) // Sắp xếp theo thứ tự
                .Select(ps => ps.Song) // Chỉ lấy Entity Song
                .ToList();
        }
    }
}