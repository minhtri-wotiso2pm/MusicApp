using System;
using System.Collections.Generic;

namespace Wotiso.MusicApp.DAL.Entities;

public partial class Playlist
{
    public int PlaylistId { get; set; }

    public int UserId { get; set; }

    public string PlaylistName { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; } = new List<PlaylistSong>();

    public virtual User User { get; set; } = null!;
}
