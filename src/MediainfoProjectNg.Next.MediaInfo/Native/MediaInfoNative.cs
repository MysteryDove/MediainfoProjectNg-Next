using System.Runtime.InteropServices;
using System.Text;

namespace MediainfoProjectNg.Next.MediaInfo.Native;

/// <summary>
/// Source-generated LibraryImport bindings for MediaInfo C API (minimal surface for projection).
/// Library name resolved via <see cref="NativeLibrary.SetDllImportResolver"/>.
/// </summary>
internal static partial class MediaInfoNative
{
    internal const string LibraryName = "MediaInfo";

    static MediaInfoNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(MediaInfoNative).Assembly, Resolve);
    }

    private static IntPtr Resolve(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!libraryName.Equals(LibraryName, StringComparison.OrdinalIgnoreCase)
            && !libraryName.Equals("mediainfo", StringComparison.OrdinalIgnoreCase))
        {
            return IntPtr.Zero;
        }

        if (NativeLibraryLoader.TryResolve(out var path)
            && NativeLibrary.TryLoad(path, out var handle))
        {
            return handle;
        }

        // Fall back to system search / name variants
        foreach (var name in new[]
                 {
                     NativeLibraryLoader.GetLibraryFileName(),
                     "MediaInfo",
                     "libmediainfo",
                     "mediainfo"
                 })
        {
            if (NativeLibrary.TryLoad(name, assembly, searchPath, out handle))
            {
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    [LibraryImport(LibraryName, EntryPoint = "MediaInfo_New")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial IntPtr New();

    [LibraryImport(LibraryName, EntryPoint = "MediaInfo_Delete")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial void Delete(IntPtr handle);

    [LibraryImport(LibraryName, EntryPoint = "MediaInfo_Close")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial void Close(IntPtr handle);

    // Windows wide-char entry points
    [LibraryImport(LibraryName, EntryPoint = "MediaInfo_Open", StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial nuint OpenW(IntPtr handle, string fileName);

    [LibraryImport(LibraryName, EntryPoint = "MediaInfoA_Open", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial nuint OpenA(IntPtr handle, string fileName);

    [LibraryImport(LibraryName, EntryPoint = "MediaInfo_Inform", StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial IntPtr InformW(IntPtr handle, nuint reserved);

    [LibraryImport(LibraryName, EntryPoint = "MediaInfoA_Inform", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial IntPtr InformA(IntPtr handle, nuint reserved);

    [LibraryImport(LibraryName, EntryPoint = "MediaInfo_Option", StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial IntPtr OptionW(IntPtr handle, string option, string value);

    [LibraryImport(LibraryName, EntryPoint = "MediaInfoA_Option", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial IntPtr OptionA(IntPtr handle, string option, string value);

    [LibraryImport(LibraryName, EntryPoint = "MediaInfo_Get", StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial IntPtr GetW(
        IntPtr handle,
        int streamKind,
        nuint streamNumber,
        string parameter,
        int kindOfInfo,
        int kindOfSearch);

    [LibraryImport(LibraryName, EntryPoint = "MediaInfoA_Get", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial IntPtr GetA(
        IntPtr handle,
        int streamKind,
        nuint streamNumber,
        string parameter,
        int kindOfInfo,
        int kindOfSearch);

    [LibraryImport(LibraryName, EntryPoint = "MediaInfo_GetI")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial IntPtr GetIW(
        IntPtr handle,
        int streamKind,
        nuint streamNumber,
        nuint parameter,
        int kindOfInfo);

    [LibraryImport(LibraryName, EntryPoint = "MediaInfoA_GetI")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial IntPtr GetIA(
        IntPtr handle,
        int streamKind,
        nuint streamNumber,
        nuint parameter,
        int kindOfInfo);

    [LibraryImport(LibraryName, EntryPoint = "MediaInfo_Count_Get")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    public static partial nuint CountGet(IntPtr handle, int streamKind, nuint streamNumber);

    public static bool UseUnicodeApi => OperatingSystem.IsWindows();

    public static nuint Open(IntPtr handle, string fileName) =>
        UseUnicodeApi ? OpenW(handle, fileName) : OpenA(handle, fileName);

    public static string Inform(IntPtr handle)
    {
        var ptr = UseUnicodeApi ? InformW(handle, 0) : InformA(handle, 0);
        return PtrToString(ptr);
    }

    public static string Option(IntPtr handle, string option, string value = "")
    {
        var ptr = UseUnicodeApi ? OptionW(handle, option, value) : OptionA(handle, option, value);
        return PtrToString(ptr);
    }

    public static string Get(IntPtr handle, StreamKind streamKind, int streamNumber, string parameter)
    {
        var ptr = UseUnicodeApi
            ? GetW(handle, (int)streamKind, (nuint)streamNumber, parameter, (int)InfoKind.Text, (int)InfoKind.Name)
            : GetA(handle, (int)streamKind, (nuint)streamNumber, parameter, (int)InfoKind.Text, (int)InfoKind.Name);
        return PtrToString(ptr);
    }

    public static string GetByIndex(IntPtr handle, StreamKind streamKind, int streamNumber, int parameter, InfoKind kind)
    {
        var ptr = UseUnicodeApi
            ? GetIW(handle, (int)streamKind, (nuint)streamNumber, (nuint)parameter, (int)kind)
            : GetIA(handle, (int)streamKind, (nuint)streamNumber, (nuint)parameter, (int)kind);
        return PtrToString(ptr);
    }

    public static int Count(IntPtr handle, StreamKind streamKind) =>
        (int)CountGet(handle, (int)streamKind, unchecked((nuint)(-1)));

    private static string PtrToString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
        {
            return string.Empty;
        }

        return UseUnicodeApi
            ? (Marshal.PtrToStringUni(ptr) ?? string.Empty)
            : (Marshal.PtrToStringUTF8(ptr) ?? string.Empty);
    }
}

internal enum StreamKind
{
    General = 0,
    Video = 1,
    Audio = 2,
    Text = 3,
    Other = 4,
    Image = 5,
    Menu = 6
}

internal enum InfoKind
{
    Name = 0,
    Text = 1,
    Measure = 2,
    Options = 3,
    NameText = 4,
    MeasureText = 5,
    Info = 6,
    HowTo = 7
}
