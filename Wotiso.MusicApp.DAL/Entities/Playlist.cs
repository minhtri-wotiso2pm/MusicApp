using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Wotiso.MusicApp.DAL.Entities;

public partial class Playlist
{
    public int PlaylistId { get; set; }

    // Thay đổi UserId thành nullable vì app không cần login
    public int? UserId { get; set; }

    public string PlaylistName { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    // For JSON storage - list of song IDs in this playlist
    [JsonInclude]
    public List<int> SongIds { get; set; } = new();

    // For EF Core (if still using database) - ignored in JSON
    [JsonIgnore]
    public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; } = new List<PlaylistSong>();

    [JsonIgnore]
    public virtual User? User { get; set; }
}
