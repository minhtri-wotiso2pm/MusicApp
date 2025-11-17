using System.Collections.Generic;
using System.Linq;
using Wotiso.MusicApp.DAL.Entities;
using Wotiso.MusicApp.DAL.Storage;

namespace Wotiso.MusicApp.DAL.Repositories
{
    /// <summary>
    /// JSON-based Song Repository - thay th? SongRepository (SQLite)
    /// </summary>
    public class JsonSongRepository
    {
        private readonly JsonDataStore _store;

        public JsonSongRepository(JsonDataStore store)
        {
            _store = store;
        }

        public List<Song> GetAll() => _store.GetAllSongs();

        public Song GetById(int id) => _store.GetSongById(id);

        public void Add(Song song)
        {
            _store.AddSong(song);
        }

        public void BulkAdd(List<Song> songs)
        {
            _store.BulkAddSongs(songs);
        }

        public void Update(Song song)
        {
            _store.UpdateSong(song);
        }

        public void Delete(int id)
        {
            _store.DeleteSong(id);
        }

        public List<Song> GetFavorites()
        {
            return _store.GetFavoriteSongs();
        }
    }
}
