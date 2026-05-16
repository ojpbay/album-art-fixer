Scan a music library directory for MP3/MP4/M4A files with missing album artwork, fetch artwork from the iTunes Search API, and write it in-place. Files are never moved, renamed, or deleted — only album art metadata is modified.

The target directory is: $ARGUMENTS

**If $ARGUMENTS is empty**, ask the user: "Which directory should I scan for missing album artwork?"

## Steps

1. **Build** the tool:
   ```
   dotnet build AlbumArtFixer/AlbumArtFixer.csproj -c Release --nologo -v quiet
   ```
   Stop and show the compiler errors if the build fails.

2. **Run** the fixer, passing the target directory:
   ```
   dotnet run --project AlbumArtFixer/AlbumArtFixer.csproj -c Release -- "<directory>"
   ```
   Stream the output live so the user can follow progress.

3. Report the final summary line (files fixed / skipped / failed).

## What the tool does
- Groups files by directory × album name, so mixed-album folders are handled correctly.
- Reuses artwork already present in the same group before calling iTunes.
- Calls `https://itunes.apple.com/search` with artist + album, rate-limited to ≤18 requests/minute.
- Retries transient failures (up to 2 retries with exponential backoff); backs off 60s on HTTP 429.
- Skips any file it cannot read or write — originals are left untouched.
