using System.Collections.Generic;
using Wotiso.MusicApp.DAL.Entities;
using Wotiso.MusicApp.DAL.Storage;

namespace Wotiso.MusicApp.DAL.Repositories
{
    /// <summary>
    /// JSON-based Playlist Repository - thay th? PlaylistRepo (SQLite)
    /// </summary>
    public class JsonPlaylistRepository
    {
        private readonly JsonDataStore _store;

        public JsonPlaylistRepository(JsonDataStore store)
        {
            _store = store;
        }

        public List<Playlist> GetAllPlaylists() => _store.GetAllPlaylists();

        public Playlist GetPlaylistById(int id) => _store.GetPlaylistById(id);

        public Playlist CreatePlaylist(Playlist playlist)
        {
            return _store.CreatePlaylist(playlist);
        }

        public void DeletePlaylist(int id)
        {
            _store.DeletePlaylist(id);
        }

        public void AddSongToPlaylist(int playlistId, int songId)
        {
            _store.AddSongToPlaylist(playlistId, songId);
        }

        public void RemoveSongFromPlaylist(int playlistId, int songId)
        {
            _store.RemoveSongFromPlaylist(playlistId, songId);
        }

        public List<Song> GetSongsForPlaylist(int playlistId)
        {
            return _store.GetSongsForPlaylist(playlistId);
        }
    }
}
