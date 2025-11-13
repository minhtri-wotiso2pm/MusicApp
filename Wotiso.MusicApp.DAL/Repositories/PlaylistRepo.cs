using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Wotiso.MusicApp.DAL.Entities;

namespace Wotiso.MusicApp.DAL.Repositories
{
    // Đảm bảo đây là 'public class'
    public class PlaylistRepo
    {
        private readonly MusicPlayerDbContext _context;

        // 1. Nhận DbContext qua constructor
        public PlaylistRepo(MusicPlayerDbContext context)
        {
            _context = context;
        }

        // 2. Lấy tất cả playlist của 1 user
        public List<Playlist> GetPlaylistsByUserId(int userId)
        {
            return _context.Playlists
                .Where(p => p.UserId == userId)
                .AsNoTracking()
                .ToList();
        }

        // 3. Tạo playlist mới
        public Playlist CreatePlaylist(Playlist playlist)
        {
            _context.Playlists.Add(playlist);
            _context.SaveChanges();
            return playlist;
        }

        // 4. Xóa playlist
        public void DeletePlaylist(int playlistId)
        {
            var playlist = _context.Playlists
                                .Include(p => p.PlaylistSongs) // Lấy cả các bài hát liên quan
                                .FirstOrDefault(p => p.PlaylistId == playlistId);

            if (playlist != null)
            {
                // Xóa các liên kết trong bảng PlaylistSong trước
                _context.PlaylistSongs.RemoveRange(playlist.PlaylistSongs);
                // Xóa playlist
                _context.Playlists.Remove(playlist);
                _context.SaveChanges();
            }
        }

        // 5. Thêm bài hát vào playlist
        public void AddSongToPlaylist(int playlistId, int songId)
        {
            // Kiểm tra xem đã tồn tại chưa
            var exists = _context.PlaylistSongs
                .Any(ps => ps.PlaylistId == playlistId && ps.SongId == songId);

            if (exists) return; // Nếu có rồi thì không làm gì

            // Lấy OrderIndex lớn nhất để thêm vào cuối
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

        // 6. Xóa bài hát khỏi playlist
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

        // 7. Lấy danh sách bài hát (Song) của 1 playlist
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