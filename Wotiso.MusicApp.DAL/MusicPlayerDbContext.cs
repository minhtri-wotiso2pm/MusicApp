using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Wotiso.MusicApp.DAL.Entities;

namespace Wotiso.MusicApp.DAL;

public partial class MusicPlayerDbContext : DbContext
{
    public MusicPlayerDbContext()
    {
    }

    public MusicPlayerDbContext(DbContextOptions<MusicPlayerDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DownloadHistory> DownloadHistories { get; set; }

    public virtual DbSet<Playlist> Playlists { get; set; }

    public virtual DbSet<PlaylistSong> PlaylistSongs { get; set; }

    public virtual DbSet<Song> Songs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    private string GetConnectionString()
    {
        IConfiguration config = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
             .Build();

        var strConn = config["ConnectionStrings:DefaultConnection"];

        if (string.IsNullOrEmpty(strConn))
            throw new Exception("Connection string not found in appsettings.json");

        return strConn;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(GetConnectionString());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DownloadHistory>(entity =>
        {
            entity.HasKey(e => e.DownloadId).HasName("PK__Download__73D5A6F0AE043932");

            entity.ToTable("DownloadHistory");

            entity.Property(e => e.DownloadedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Song).WithMany(p => p.DownloadHistories)
                .HasForeignKey(d => d.SongId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DownloadH__SongI__38996AB5");

            entity.HasOne(d => d.User).WithMany(p => p.DownloadHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DownloadH__UserI__37A5467C");
        });

        modelBuilder.Entity<Playlist>(entity =>
        {
            entity.HasKey(e => e.PlaylistId).HasName("PK__Playlist__B30167A0804B0243");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PlaylistName).HasMaxLength(200);

            entity.HasOne(d => d.User).WithMany(p => p.Playlists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Playlists__UserI__2C3393D0");
        });

        modelBuilder.Entity<PlaylistSong>(entity =>
        {
            entity.HasKey(e => new { e.PlaylistId, e.SongId }).HasName("PK__Playlist__D22F5AC9C3E07E1F");

            entity.HasOne(d => d.Playlist).WithMany(p => p.PlaylistSongs)
                .HasForeignKey(d => d.PlaylistId)
                .HasConstraintName("FK__PlaylistS__Playl__2F10007B");

            entity.HasOne(d => d.Song).WithMany(p => p.PlaylistSongs)
                .HasForeignKey(d => d.SongId)
                .HasConstraintName("FK__PlaylistS__SongI__300424B4");
        });

        modelBuilder.Entity<Song>(entity =>
        {
            entity.HasKey(e => e.SongId).HasName("PK__Songs__12E3D69793A5FFA2");

            entity.Property(e => e.Album).HasMaxLength(150);
            entity.Property(e => e.Artist).HasMaxLength(150);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(10);
            entity.Property(e => e.Title).HasMaxLength(200);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C803B7196");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534B2716D4E").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.UserName).HasMaxLength(100);

            entity.HasMany(d => d.Songs).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "Favorite",
                    r => r.HasOne<Song>().WithMany()
                        .HasForeignKey("SongId")
                        .HasConstraintName("FK__Favorites__SongI__33D4B598"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK__Favorites__UserI__32E0915F"),
                    j =>
                    {
                        j.HasKey("UserId", "SongId").HasName("PK__Favorite__76A6F1259FE6E131");
                        j.ToTable("Favorites");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
