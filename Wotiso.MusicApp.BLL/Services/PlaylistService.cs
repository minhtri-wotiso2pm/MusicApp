using System;
using System.Collections.Generic;
using Wotiso.MusicApp.DAL.Entities;
using Wotiso.MusicApp.DAL.Repositories;

namespace Wotiso.MusicApp.BLL.Services
{
    public class PlaylistService
    {
        private readonly JsonPlaylistRepository _playlistRepo;

        public PlaylistService(JsonPlaylistRepository playlistRepo)
        {
            _playlistRepo = playlistRepo;
        }

        // Lấy tất cả playlist (không phân biệt user)
        public List<Playlist> GetAllPlaylists()
        {
            return _playlistRepo.GetAllPlaylists();
        }

        // DEPRECATED: Giữ lại để backward compatible nhưng gọi GetAllPlaylists()
        [Obsolete("Use GetAllPlaylists() instead. UserId is no longer needed.")]
        public List<Playlist> GetPlaylistsForUser(int userId)
        {
            return GetAllPlaylists();
        }

        // Tạo playlist mới (không cần userId)
        public Playlist CreateNewPlaylist(string playlistName)
        {
            if (string.IsNullOrWhiteSpace(playlistName))
            {
                throw new Exception("Tên playlist không được để trống.");
            }

            var newPlaylist = new Playlist
            {
                UserId = null, // Set null vì không có user
                PlaylistName = playlistName,
                CreatedAt = DateTime.Now
            };

            return _playlistRepo.CreatePlaylist(newPlaylist);
        }

        // DEPRECATED: Giữ lại để backward compatible
        [Obsolete("Use CreateNewPlaylist(string) instead. UserId is no longer needed.")]
        public Playlist CreateNewPlaylist(int userId, string playlistName)
        {
            return CreateNewPlaylist(playlistName);
        }

        public void DeletePlaylist(int playlistId)
        {
            _playlistRepo.DeletePlaylist(playlistId);
        }

        public void AddSongToPlaylist(int playlistId, int songId)
        {
            _playlistRepo.AddSongToPlaylist(playlistId, songId);
        }

        public void RemoveSongFromPlaylist(int playlistId, int songId)
        {
            _playlistRepo.RemoveSongFromPlaylist(playlistId, songId);
        }

        public List<Song> GetSongsForPlaylist(int playlistId)
        {
            return _playlistRepo.GetSongsForPlaylist(playlistId);
        }
    }
}