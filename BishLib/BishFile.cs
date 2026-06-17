using System.Text;
using BishRuntime;

namespace BishLib;

public struct BishFileModule : IModule
{
    public static BishObject Exports => IModule.ExportsFrom(
        ("Reader", BishReader.StaticType),
        ("Writer", BishWriter.StaticType),
        ("FileError", Error)
    );

    public static readonly BishType Error = new("FileError", [BishError.StaticType]);

    public static Encoding EncodingFrom(BishString? encoding) =>
        encoding is null ? Encoding.UTF8 : Encoding.GetEncoding(encoding.Value);
}

public class BishReader(StreamReader reader) : BishObject
{
    public readonly StreamReader Reader = reader;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Reader");

    [Builtin("hook")]
    public static BishReader New(BishString path, [DefaultNull] BishString? encoding) =>
        new(BishException.Wrapped(BishFileModule.Error,
            () => new StreamReader(path.Value, BishFileModule.EncodingFrom(encoding))));

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

    [Builtin("hook")]
    public static BishWriter New(BishString path, [DefaultNull] BishBool? append, [DefaultNull] BishString? encoding) =>
        new(BishException.Wrapped(BishFileModule.Error,
            () => new StreamWriter(path.Value, append: append?.Value ?? false, BishFileModule.EncodingFrom(encoding))));

    [Builtin]
    public static void Dispose(BishWriter self) => self.Writer.Dispose();

    [Builtin]
    public static BishNativeTask Write(BishWriter self, BishObject content) =>
        new(() => self.Writer.WriteAsync(BishString.CallShow(content)));
}