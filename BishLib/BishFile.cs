using System.Text;
using BishRuntime;

namespace BishLib;

public struct BishFileModule : IModule
{
    public static BishObject Exports => IModule.ExportsFrom(
        ("Path", BishPath.StaticType),
        ("Reader", BishReader.StaticType),
        ("Writer", BishWriter.StaticType),
        ("FileError", Error)
    );

    public static readonly BishType Error = new("FileError", [BishError.StaticType]);

    public static Encoding EncodingFrom(string? encoding) =>
        encoding is null ? Encoding.UTF8 : Encoding.GetEncoding(encoding);
}

public class BishPath(string value) : BishObject
{
    public readonly string Value = value;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Path");

    [Builtin("hook")]
    public static BishPath New(BishString value) => new(value.Value);

    [Builtin]
    public static BishString Repr(BishPath self, BishReprContext ctx) =>
        new($"Path({BishString.CallRepr(self.Value, ctx)})");

    [Builtin("hook")]
    public static BishString Get_value(BishPath self) => new(self.Value);

    [Builtin("hook")]
    public static BishString Get_name(BishPath self) => new(Path.GetFileName(self.Value));

    [Builtin("hook")]
    public static BishString Get_stem(BishPath self) => new(Path.GetFileNameWithoutExtension(self.Value));

    [Builtin("hook")]
    public static BishString Get_ext(BishPath self) => new(Path.GetExtension(self.Value));

    [Builtin("hook")]
    public static BishPath? Get_dir(BishPath self) =>
        Path.GetDirectoryName(self.Value) is { } result ? new BishPath(result) : null;

    [Builtin("hook")]
    public static BishPath? Get_root(BishPath self) =>
        Path.GetPathRoot(self.Value) is { } result ? new BishPath(result) : null;

    [Builtin]
    public static BishPath WithExt(BishPath self, [DefaultNull] BishString? ext) =>
        new(Path.ChangeExtension(self.Value, ext?.Value));

    [Builtin("op")]
    public static BishPath Div(BishPath self, BishString other) => new(Path.Join(self.Value, other.Value));

    [Builtin("hook")]
    public static BishPath Get_full(BishPath self) => new(Path.GetFullPath(self.Value));

    [Builtin]
    public static BishPath Relative(BishPath self, BishPath from) => new(Path.GetRelativePath(from.Value, self.Value));

    [Builtin("hook")]
    public static BishBool Get_isRelative(BishPath self) => BishBool.Of(!Path.IsPathRooted(self.Value));

    [Builtin("hook")]
    public static BishString Get_sep(BishType _) => new(Path.DirectorySeparatorChar);

    [Builtin("hook")]
    public static BishPath Get_cwd(BishType _) => new(Environment.CurrentDirectory);

    [Builtin("hook")]
    public static BishPath Get_home(BishType _) =>
        new(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    [Builtin("hook")]
    public static BishPath Get_temp(BishType _) => new(Path.GetTempPath());

    // File operations

    [Builtin("hook")]
    public static BishBool Get_exists(BishPath self) => BishBool.Of(File.Exists(self.Value));

    [Builtin]
    public static void Create(BishPath self) =>
        BishException.Wrapped(BishFileModule.Error, () => File.Create(self.Value).Dispose());

    [Builtin]
    public static void Delete(BishPath self) =>
        BishException.Wrapped(BishFileModule.Error, () => File.Delete(self.Value));

    [Builtin]
    public static void CopyTo(BishPath self, BishPath dest, [DefaultNull] BishBool? overwrite) =>
        BishException.Wrapped(BishFileModule.Error, () => File.Copy(self.Value, dest.Value, overwrite?.Value ?? false));

    [Builtin]
    public static void MoveTo(BishPath self, BishPath dest, [DefaultNull] BishBool? overwrite) =>
        BishException.Wrapped(BishFileModule.Error, () => File.Move(self.Value, dest.Value, overwrite?.Value ?? false));

    [Builtin]
    public static BishReader Read(BishPath self, [DefaultNull] BishString? encoding) =>
        BishReader.Open(self.Value, encoding?.Value);

    [Builtin]
    public static BishWriter Write(BishPath self, [DefaultNull] BishBool? append, [DefaultNull] BishString? encoding) =>
        BishWriter.Open(self.Value, append?.Value, encoding?.Value);

    [Builtin]
    public static BishFrame ReadBytecodes(BishPath self) =>
        BishException.Wrapped(BishFileModule.Error, () =>
        {
            using var stream = File.OpenRead(self.Value);
            return stream.ReadBytecodes();
        });

    [Builtin]
    public static void WriteBytecodes(BishPath self, BishFrame frame) =>
        BishException.Wrapped(BishFileModule.Error, () =>
        {
            using var stream = File.Create(self.Value);
            stream.WriteBytecodes(frame);
        });

    // Directory operations
    [Builtin("hook")]
    public static BishBool Get_existsDir(BishPath self) => BishBool.Of(Directory.Exists(self.Value));

    [Builtin]
    public static void CreateDir(BishPath self) =>
        BishException.Wrapped(BishFileModule.Error, () => Directory.CreateDirectory(self.Value));

    [Builtin]
    public static void DeleteDir(BishPath self, [DefaultNull] BishBool? recursive) =>
        BishException.Wrapped(BishFileModule.Error, () => Directory.Delete(self.Value, recursive?.Value ?? false));

    [Builtin]
    public static void CopyDirTo(BishPath self, BishPath dest, [DefaultNull] BishBool? overwrite) =>
        BishException.Wrapped(BishFileModule.Error,
            () => Directory.Copy(self.Value, dest.Value, overwrite?.Value ?? false));

    [Builtin]
    public static void MoveDirTo(BishPath self, BishPath dest, [DefaultNull] BishBool? overwrite) =>
        BishException.Wrapped(BishFileModule.Error,
            () => Directory.Move(self.Value, dest.Value, overwrite?.Value ?? false));

    [Builtin("hook")]
    public static BishNativeIterator Get_children(BishPath self) =>
        new(BishException.Wrapped(BishFileModule.Error, () => Directory.EnumerateFileSystemEntries(self.Value))
            .Select(entry => BishException.Wrapped(BishFileModule.Error, () => new BishPath(entry))));
}

public static class DirectoryHelper
{
    extension(Directory)
    {
        public static void Copy(string source, string dest, bool overwrite = false)
        {
            var dir = new DirectoryInfo(source);
            if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory does not exist: {source}");
            if (!Directory.Exists(dest)) Directory.CreateDirectory(dest);
            foreach (var sub in dir.GetFiles())
                sub.CopyTo(Path.Combine(dest, sub.Name), overwrite);
            foreach (var sub in dir.GetDirectories())
                Directory.Copy(sub.FullName, Path.Combine(dest, sub.Name), overwrite);
        }

        public static void Move(string source, string dest, bool overwrite = false)
        {
            var sourceRoot = Path.GetPathRoot(Path.GetFullPath(source))?.ToLowerInvariant();
            var destRoot = Path.GetPathRoot(Path.GetFullPath(dest))?.ToLowerInvariant();
            if (sourceRoot == destRoot && !Directory.Exists(dest))
                Directory.Move(sourceDirName: source, destDirName: dest);
            else
            {
                Directory.Copy(source, dest, overwrite);
                Directory.Delete(source, true);
            }
        }
    }
}

public class BishReader(StreamReader reader) : BishObject
{
    public readonly StreamReader Reader = reader;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Reader");

    public static BishReader Open(string path, string? encoding) => new(BishException.Wrapped(BishFileModule.Error,
        () => new StreamReader(path, BishFileModule.EncodingFrom(encoding))));

    [Builtin]
    public static void Dispose(BishReader self) => self.Reader.Dispose();

    private async Task<char?> ReadCharAsync()
    {
        var buffer = new char[1];
        if (await Reader.ReadAsync(buffer, 0, 1) == 0) return null;
        return buffer[0];
    }

    internal BishNativeTask ReadCharIter() => new(async () =>
        await BishException.Wrapped(BishFileModule.Error, ReadCharAsync()) is { } c
            ? new BishString(c)
            : BishIteratorStop.Instance);

    [Builtin]
    public static BishNativeTask ReadChar(BishReader self) => new(async () =>
        await BishException.Wrapped(BishFileModule.Error, self.ReadCharAsync()) is { } c ? new BishString(c) : null);

    [Builtin("hook")]
    public static BishFileChars Get_chars(BishReader self) => new(self);

    private Task<string?> ReadLineAsync() => Reader.ReadLineAsync();

    internal BishNativeTask ReadLineIter() => new(async () =>
        await BishException.Wrapped(BishFileModule.Error, ReadLineAsync()) is { } c
            ? new BishString(c)
            : BishIteratorStop.Instance);

    [Builtin]
    public static BishNativeTask ReadLine(BishReader self) => new(async () =>
        await BishException.Wrapped(BishFileModule.Error, self.ReadLineAsync) is { } l ? new BishString(l) : null);

    [Builtin("hook")]
    public static BishFileLines Get_lines(BishReader self) => new(self);

    [Builtin]
    public static BishNativeTask ReadAll(BishReader self) => new(async () =>
        new BishString(await BishException.Wrapped(BishFileModule.Error, self.Reader.ReadToEndAsync())));
}

public class BishFileChars(BishReader reader) : BishObject
{
    public BishReader Reader => reader;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("file.char.iter", [BishIterator.AsyncType]);

    [Iter]
    public BishNativeTask Next() => Reader.ReadCharIter();
}

public class BishFileLines(BishReader reader) : BishObject
{
    public BishReader Reader => reader;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("file.line.iter", [BishIterator.AsyncType]);

    [Iter]
    public BishNativeTask Next() => Reader.ReadLineIter();
}

public class BishWriter(StreamWriter writer) : BishObject
{
    public readonly StreamWriter Writer = writer;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Writer");

    public static BishWriter Open(string path, bool? append, string? encoding) => new(
        BishException.Wrapped(BishFileModule.Error,
            () => new StreamWriter(path, append ?? false, BishFileModule.EncodingFrom(encoding))));

    [Builtin]
    public static void Dispose(BishWriter self) => self.Writer.Dispose();

    [Builtin]
    public static BishNativeTask Write(BishWriter self, BishString content) =>
        new(() => self.Writer.WriteAsync(content.Value));
}