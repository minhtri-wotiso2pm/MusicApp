using System;
using System.Collections.Generic;

namespace Wotiso.MusicApp.DAL.Entities;

public partial class DownloadHistory
{
    public int DownloadId { get; set; }

    public int UserId { get; set; }

    public int SongId { get; set; }

    public DateTime? DownloadedAt { get; set; }

    public virtual Song Song { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
