namespace AlbumArtFixer;

sealed class AlbumArtFixerApp(AppConfig config)
{
    public async Task RunAsync(string rootDirectory)
    {
        Console.WriteLine($"Scanning {rootDirectory}...");

        var allFiles = Directory
            .EnumerateFiles(rootDirectory, "*.*", SearchOption.AllDirectories)
            .Where(f => config.SupportedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToList();

        Console.WriteLine($"Found {allFiles.Count} audio file(s)\n");
        if (allFiles.Count == 0) return;

        var groups = BuildAlbumGroups(allFiles);

        using var iTunes = new ItunesArtworkClient();
        int totalFixed = 0, totalSkipped = 0, totalFailed = 0;

        foreach (var group in groups)
        {
            var relDir = Path.GetRelativePath(rootDirectory, group.Directory);
            var label = string.IsNullOrWhiteSpace(group.Album) ? relDir : $"{relDir} / \"{group.Album}\"";

            var needsArt = group.Files.Where(f => !AudioMetadata.HasArtwork(f)).ToList();
            if (needsArt.Count == 0)
            {
                Console.WriteLine($"[ok] {label}");
                continue;
            }

            Console.WriteLine($"[..] {label}  ({needsArt.Count}/{group.Files.Count} missing artwork)");

            // Reuse existing artwork from a sibling in the same group, if one exists
            var needsSet = new HashSet<string>(needsArt, StringComparer.OrdinalIgnoreCase);
            var sourceFile = group.Files.FirstOrDefault(f => !needsSet.Contains(f));
            byte[]? artwork = sourceFile is not null ? AudioMetadata.GetArtwork(sourceFile) : null;

            if (artwork is not null)
            {
                Console.WriteLine($"  Reusing artwork from {Path.GetFileName(sourceFile!)}");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(group.Artist) && string.IsNullOrWhiteSpace(group.Album))
                {
                    Console.WriteLine("  Skip: no artist/album metadata to search with");
                    totalSkipped += needsArt.Count;
                    continue;
                }

                Console.WriteLine($"  Searching iTunes: \"{group.Artist}\" — \"{group.Album}\"");
                artwork = await iTunes.FindArtworkAsync(group.Artist, group.Album);

                if (artwork is null)
                {
                    Console.WriteLine("  Not found on iTunes");
                    totalSkipped += needsArt.Count;
                    continue;
                }

                Console.WriteLine($"  Downloaded {artwork.Length / 1024}KB artwork");
            }

            foreach (var file in needsArt)
            {
                if (AudioMetadata.WriteArtwork(file, artwork))
                {
                    Console.WriteLine($"  + {Path.GetFileName(file)}");
                    totalFixed++;
                }
                else
                {
                    totalFailed++;
                }
            }
        }

        Console.WriteLine($"\nDone: {totalFixed} fixed, {totalSkipped} skipped, {totalFailed} failed");
    }

    // Groups files by (directory, album name) so that mixed-album directories
    // (e.g. an artist folder without per-album sub-folders) are handled correctly.
    // Files with no album tag share a group keyed to the empty string.
    private static List<AlbumGroup> BuildAlbumGroups(List<string> files)
    {
        return files
            .Select(f =>
            {
                var (artist, album) = AudioMetadata.GetAlbumInfo(f);
                return new { Path = f, Artist = artist, Album = album, Dir = Path.GetDirectoryName(f)! };
            })
            .GroupBy(x => (x.Dir, AlbumKey: x.Album.Trim().ToLowerInvariant()))
            .Select(g =>
            {
                // Pick the most-frequently occurring non-empty artist and the original-cased album name
                var dominant = g
                    .GroupBy(x => x.Artist.Trim().ToLowerInvariant())
                    .OrderByDescending(x => x.Count())
                    .First()
                    .First();
                return new AlbumGroup(
                    Directory: g.Key.Dir,
                    Artist: dominant.Artist.Trim(),
                    Album: dominant.Album.Trim(),
                    Files: g.Select(x => x.Path).ToList()
                );
            })
            .ToList();
    }
}

record AlbumGroup(string Directory, string Artist, string Album, List<string> Files);
