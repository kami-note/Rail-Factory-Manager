namespace RailFactory.Frontend.Api;

public static class MaterialImageUploadPolicy
{
    public const int MaxUploadBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp"
    };

    public static bool TryGetExtension(string contentType, out string extension) =>
        AllowedMimeTypes.TryGetValue(contentType, out extension!);
}
