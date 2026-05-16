namespace AlbumArtFixer;

static class AudioMetadata
{
    public static bool HasArtwork(string path)
    {
        try
        {
            using var file = TagLib.File.Create(path);
            return file.Tag.Pictures.Length > 0;
        }
        catch { return false; }
    }

    public static (string artist, string album) GetAlbumInfo(string path)
    {
        try
        {
            using var file = TagLib.File.Create(path);
            return (
                file.Tag.FirstAlbumArtist ?? file.Tag.FirstPerformer ?? "",
                file.Tag.Album ?? ""
            );
        }
        catch { return ("", ""); }
    }

    public static byte[]? GetArtwork(string path)
    {
        try
        {
            using var file = TagLib.File.Create(path);
            return file.Tag.Pictures.Length > 0 ? file.Tag.Pictures[0].Data.Data : null;
        }
        catch { return null; }
    }

    // Returns true only if the artwork was written successfully.
    // Any exception leaves the file untouched (TagLib# does not partially commit writes).
    public static bool WriteArtwork(string path, byte[] imageData)
    {
        try
        {
            using var file = TagLib.File.Create(path);
            var picture = new TagLib.Picture
            {
                Type    = TagLib.PictureType.FrontCover,
                MimeType = DetectMimeType(imageData),
                Data    = new TagLib.ByteVector(imageData),
            };
            file.Tag.Pictures = [picture];
            file.Save();
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  Write failed for {Path.GetFileName(path)}: {ex.Message}");
            return false;
        }
    }

    private static string DetectMimeType(byte[] data) =>
        data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
            ? "image/png"
            : "image/jpeg";
}
