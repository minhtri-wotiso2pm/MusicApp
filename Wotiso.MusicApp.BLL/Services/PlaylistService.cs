using System;
using System.Collections.Generic;
using Wotiso.MusicApp.DAL.Entities;
using Wotiso.MusicApp.DAL.Repositories;

namespace Wotiso.MusicApp.BLL.Services
{
    public class PlaylistService
    {
        private readonly PlaylistRepo _playlistRepo;

        public PlaylistService(PlaylistRepo playlistRepo)
        {
            _playlistRepo = playlistRepo;
        }

        public List<Playlist> GetPlaylistsForUser(int userId)
        {
            return _playlistRepo.GetPlaylistsByUserId(userId);
        }

        public Playlist CreateNewPlaylist(int userId, string playlistName)
        {
            if (string.IsNullOrWhiteSpace(playlistName))
            {
                throw new Exception("Tên playlist không được để trống.");
            }

            var newPlaylist = new Playlist
            {
                UserId = userId,
                PlaylistName = playlistName,
                CreatedAt = DateTime.Now
            };

            return _playlistRepo.CreatePlaylist(newPlaylist);
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