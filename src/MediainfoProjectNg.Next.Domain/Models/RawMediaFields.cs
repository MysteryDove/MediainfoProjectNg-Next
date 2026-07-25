using System.Globalization;

namespace MediainfoProjectNg.Next.Domain.Models;

/// <summary>
/// Presence-preserving raw MediaInfo field. Null <see cref="RawText"/> means absent.
/// Explicit empty string is present-empty; malformed parse is flagged separately.
/// </summary>
public readonly record struct RawField(string? RawText, bool ParseFailed = false)
{
    public bool IsPresent => RawText is not null;
    public bool IsAbsent => RawText is null;
    public bool IsPresentEmpty => RawText is { Length: 0 };
    public string TextOrEmpty => RawText ?? string.Empty;

    public static RawField Absent => new(null);
    public static RawField Of(string? raw) => raw is null ? Absent : new(raw);
    public static RawField Malformed(string? raw) => new(raw, ParseFailed: true);

    public RawField ParseLong(out long? value)
    {
        value = null;
        if (!IsPresent || IsPresentEmpty)
        {
            return this;
        }

        try
        {
            if (!decimal.TryParse(TextOrEmpty, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                return Malformed(RawText);
            }

            // Collation evidence (Width/Height/BitDepth): nonzero fraction → ParseFailed, no silent truncate.
            if (parsed != decimal.Truncate(parsed))
            {
                return Malformed(RawText);
            }

            if (parsed > long.MaxValue || parsed < long.MinValue)
            {
                return Malformed(RawText);
            }

            value = (long)parsed;
            return this;
        }
        catch (OverflowException)
        {
            value = null;
            return Malformed(RawText);
        }
    }
}

/// <summary>
/// Immutable raw MediaInfo snapshot used by Collation rules. Display models may
/// continue to show legacy-friendly values; rules consume this evidence.
/// </summary>
public sealed record RawMediaSnapshot
{
    public required string FullPath { get; init; }
    public required string Extension { get; init; }
    public required RawField ContainerFormat { get; init; }
    public IReadOnlyList<RawVideoTrack> VideoTracks { get; init; } = [];
    public IReadOnlyList<RawAudioTrack> AudioTracks { get; init; } = [];
    public IReadOnlyList<RawTextTrack> TextTracks { get; init; } = [];
    public IReadOnlyList<RawChapter> Chapters { get; init; } = [];
    public long ChapterCount { get; init; }
    public bool AdapterUnavailable { get; init; }
}

public sealed record RawVideoTrack
{
    public RawField Format { get; init; }
    public RawField FormatProfile { get; init; }
    public RawField Width { get; init; }
    public RawField Height { get; init; }
    public RawField BitDepth { get; init; }
    public RawField ColorSpace { get; init; }
    public RawField ChromaSubsampling { get; init; }
    public RawField Language { get; init; }
    public RawField Default { get; init; }
    public RawField ScanType { get; init; }
    public RawField FrameRateMode { get; init; }
    public RawField FrameRate { get; init; }
    public RawField ColourRange { get; init; }
    public RawField MatrixCoefficients { get; init; }
    public RawField ColourPrimaries { get; init; }
    public RawField TransferCharacteristics { get; init; }
    public long? ParsedWidth { get; init; }
    public long? ParsedHeight { get; init; }
    public long? ParsedBitDepth { get; init; }
}

public sealed class RawAudioTrack
{
    public RawField Format { get; init; }
    public RawField Language { get; init; }
    public RawField Default { get; init; }
}

public sealed class RawTextTrack
{
    public RawField Format { get; init; }
    public RawField Language { get; init; }
    public RawField Default { get; init; }
}

public sealed class RawChapter
{
    public RawField Language { get; init; }
    public RawField Name { get; init; }
    public int? TimespanMs { get; init; }
}
