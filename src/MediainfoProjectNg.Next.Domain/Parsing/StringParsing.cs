namespace MediainfoProjectNg.Next.Domain.Parsing;

public static class StringParsing
{
    public static long TryParseAsLong(this string? s) =>
        s is not null && decimal.TryParse(s, out var i) ? (long)i : 0;

    public static int TryParseAsMillisecond(this string? s) =>
        s is not null && TimeSpan.TryParse(s, out var ts) ? (int)ts.TotalMilliseconds : 0;
}
