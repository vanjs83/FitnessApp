using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FitnessApp.Infrastructure.Persistence.Converters;

/// <summary>
/// Builds an EF Core value converter that stores only the bare file name for uploaded media while
/// the application keeps working with the full public URL. Storing just the file name keeps rows
/// portable — moving to a CDN is only a change of the public URL prefix, with no row rewrites.
/// </summary>
public static class ImageFileNameConverter
{
    /// <param name="urlPrefix">Public URL prefix for this media kind, e.g. "/uploads/progress".</param>
    public static ValueConverter For(string urlPrefix)
    {
        var prefix = urlPrefix.TrimEnd('/') + "/";
        return new ValueConverter<string, string>(
            value => ToStored(value, prefix),
            value => ToPublicUrl(value, prefix));
    }

    // Persisted form: drop our own upload prefix so only the file name is stored. External URLs
    // (e.g. a YouTube link) and already-bare names don't match the prefix and are kept unchanged.
    private static string ToStored(string value, string prefix) =>
        value.StartsWith(prefix, StringComparison.Ordinal) ? value[prefix.Length..] : value;

    // Public form: rebuild a bare file name into "/prefix/file". Legacy rows that still hold a full
    // path or URL contain a '/', so they pass through untouched (no data migration needed).
    private static string ToPublicUrl(string value, string prefix) =>
        value.Length > 0 && !value.Contains('/') ? prefix + value : value;
}
