# Album Art Fixer

A .NET 8 console application that recursively scans a music library for MP3, MP4, and M4A files missing album artwork, fetches matching artwork from the iTunes Search API, and writes it back to the files in-place — no files are moved or renamed.

## Features

- Recursively scans a directory for audio files (MP3, MP4, M4A)
- Groups files by album so artwork is fetched once per album, not per track
- Reuses artwork already present in any track of the same album — avoids unnecessary API calls
- Fetches high-resolution (600×600) artwork from the iTunes Search API
- Writes artwork directly into file metadata without altering file structure
- Rate-limited to stay within iTunes API limits (~18 requests/minute)

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Usage

```bash
# Build
dotnet build AlbumArtFixer/AlbumArtFixer.csproj

# Run against a music library directory
dotnet run --project AlbumArtFixer/AlbumArtFixer.csproj -- "<path-to-music-library>"
```

Replace `<path-to-music-library>` with the root folder of your music collection. The app will scan all subdirectories.

## How It Works

1. Scans the target directory recursively for supported audio files.
2. Groups files by `(directory, album name)` — handles artist folders containing multiple albums.
3. For each group, checks if any track already has artwork. If so, copies it to all sibling tracks.
4. For groups with no artwork, queries the iTunes Search API using the artist and album name.
5. Downloads the best matching artwork and writes it to all tracks in the group.

## Dependencies

- [TagLibSharp 2.3.0](https://github.com/mono/taglib-sharp) — reads and writes audio file metadata
- System.Text.Json (built-in) — parses iTunes API responses
