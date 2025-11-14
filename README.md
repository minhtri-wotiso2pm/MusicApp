# MusicApp
DATABASE


-- Tạo database
CREATE DATABASE MusicPlayerDB;
GO

USE MusicPlayerDB;
GO

-- Bảng người dùng (User)
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) UNIQUE,
    PasswordHash NVARCHAR(255),
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Bảng bài hát (Song)
CREATE TABLE Songs (
    SongId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Artist NVARCHAR(150),
    Album NVARCHAR(150),
    FilePath NVARCHAR(500) NOT NULL,
    Duration INT, -- Thời lượng (giây)
    FileType NVARCHAR(10), -- mp3, mp4, wav...
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Bảng Playlist
CREATE TABLE Playlists (
    PlaylistId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    PlaylistName NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

-- Bảng PlaylistSong (liên kết nhiều-nhiều giữa Playlist và Song)
CREATE TABLE PlaylistSongs (
    PlaylistId INT NOT NULL,
    SongId INT NOT NULL,
    OrderIndex INT, -- Thứ tự bài trong playlist
    PRIMARY KEY (PlaylistId, SongId),
    FOREIGN KEY (PlaylistId) REFERENCES Playlists(PlaylistId) ON DELETE CASCADE,
    FOREIGN KEY (SongId) REFERENCES Songs(SongId) ON DELETE CASCADE
);

-- Bảng Favorite (Yêu thích)
CREATE TABLE Favorites (
    UserId INT NOT NULL,
    SongId INT NOT NULL,
    PRIMARY KEY (UserId, SongId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (SongId) REFERENCES Songs(SongId) ON DELETE CASCADE
);

-- Bảng DownloadHistory (lịch sử tải bài hát)
CREATE TABLE DownloadHistory (
    DownloadId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    SongId INT NOT NULL,
    DownloadedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (SongId) REFERENCES Songs(SongId)
);

-- Dữ liệu mẫu (nếu muốn test)
INSERT INTO Users (UserName, Email, PasswordHash)
VALUES (N'Trí', N'tri@example.com', N'123456');

INSERT INTO Songs (Title, Artist, Album, FilePath, Duration, FileType)
VALUES 
(N'Test Song 1', N'Artist A', N'Album A', N'C:\Music\Test1.mp3', 240, N'mp3'),
(N'Test Song 2', N'Artist B', N'Album B', N'C:\Music\Test2.wav', 200, N'wav'),
(N'Test Song 3', N'Artist C', N'Album C', N'C:\Music\Test3.mp4', 300, N'mp4');

INSERT INTO Playlists (UserId, PlaylistName)
VALUES (1, N'Yêu thích của tôi');

INSERT INTO PlaylistSongs (PlaylistId, SongId, OrderIndex)
VALUES (1, 1, 1), (1, 2, 2);

INSERT INTO Favorites (UserId, SongId)
VALUES (1, 1), (1, 3);

GO






ALTER TABLE Songs
ADD IsFavorite BIT NOT NULL DEFAULT 0;
