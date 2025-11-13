using System;
using System.Collections.Generic;

namespace Wotiso.MusicApp.DAL.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string? Email { get; set; }

    public string? PasswordHash { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<DownloadHistory> DownloadHistories { get; set; } = new List<DownloadHistory>();

    public virtual ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();

    public virtual ICollection<Song> Songs { get; set; } = new List<Song>();
}
