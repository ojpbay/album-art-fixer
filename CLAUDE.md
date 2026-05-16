# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

`album-art-fixer` is a .NET 8 console application that recursively scans a music library for MP3/MP4/M4A files missing album artwork, fetches artwork from the iTunes Search API, and writes it in-place without moving or renaming any files.

## Commands

```bash
dotnet build AlbumArtFixer/AlbumArtFixer.csproj        # build
dotnet run --project AlbumArtFixer/AlbumArtFixer.csproj -- "<dir>"  # run against a directory
dotnet test                                             # no tests yet
```

Run the `/fix-album-art` skill to build and run against a user-supplied directory in one step.

## Architecture

```
AlbumArtFixer/
  Program.cs              Entry point — validates args, delegates to AlbumArtFixerApp
  AlbumArtFixerApp.cs     Orchestration: scans files, groups by (directory × album), drives the fix loop
  AudioMetadata.cs        TagLib# wrapper — all read/write; exceptions leave files untouched
  ItunesArtworkClient.cs  iTunes Search API client with rate limiting and retry
  RateLimiter.cs          Token-bucket limiter (≤18 req/min by default, sequential callers only)
```

### Key design decisions

- **Grouping**: files are grouped by `(directory, album_name_lowercase)`, not just directory. This handles artist folders that contain multiple albums without per-album sub-folders.
- **Artwork reuse**: before calling iTunes, the app checks whether any file in the same group already has artwork. If so, that artwork is reused for all siblings — no API call needed.
- **Safety**: `AudioMetadata.WriteArtwork` wraps `file.Save()` in a try/catch. TagLib# does not partially commit writes for the formats supported, so an exception leaves the file unchanged.
- **Rate limiting**: iTunes Search API allows ~20 calls/minute. The limiter enforces a minimum interval between calls (`60s / 18 = 3.3s`). HTTP 429 responses trigger an additional 60s back-off.
- **iTunes API**: `https://itunes.apple.com/search?term={artist}+{album}&entity=album&media=music&limit=5` — artwork URL `100x100bb` is replaced with `600x600bb` for higher resolution.

## Dependencies

- **TagLibSharp 2.3.0** — reading and writing audio file metadata (ID3v2 for MP3, `covr` atom for MP4/M4A).
- **System.Text.Json** (built-in) — parsing iTunes API responses.
