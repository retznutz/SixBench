namespace SixBench.Services.Certificates;

/// <summary>
/// Writes key material: atomically (temp file, then move) and, on macOS and Linux, readable only by the server's user.
/// </summary>
public static class SecureFile
{
    /// <summary>Writes <paramref name="contents"/> to <paramref name="path"/>, creating the directory if needed.</summary>
    /// <param name="path">File path.</param>
    /// <param name="contents">Bytes to write.</param>
    public static void Write(string path, byte[] contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        using (var stream = new FileStream(temp, options))
        {
            stream.Write(contents);
        }

        File.Move(temp, path, overwrite: true);
    }
}
