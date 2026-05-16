using AlbumArtFixer;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: album-art-fixer <directory>");
    return 1;
}

if (!Directory.Exists(args[0]))
{
    Console.Error.WriteLine($"Directory not found: {args[0]}");
    return 1;
}

var config = AppConfig.Load();
await new AlbumArtFixerApp(config).RunAsync(args[0]);
return 0;
