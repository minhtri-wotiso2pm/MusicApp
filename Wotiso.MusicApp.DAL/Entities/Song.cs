using System;
using System.Collections.Generic;

namespace Wotiso.MusicApp.DAL.Entities;

public partial class Song
{
    public int SongId { get; set; }

    public string Title { get; set; } = null!;

    public string? Artist { get; set; }

    public string? Album { get; set; }

    public string FilePath { get; set; } = null!;

    public int? Duration { get; set; }

    public string? FileType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<DownloadHistory> DownloadHistories { get; set; } = new List<DownloadHistory>();

    public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; } = new List<PlaylistSong>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public bool IsFavorite { get; set; } = false;

}
