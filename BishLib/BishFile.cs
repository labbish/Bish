using System.Text;
using BishRuntime;

namespace BishLib;

public static class BishFileModule
{
    public static void Initialize() => BishLib.InitializeModule("file",
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

    public BishString? ReadChar() => BishException.Wrapped(BishFileModule.Error, Reader.Read) switch
    {
        -1 => null,
        var chr => new BishString((char)chr)
    };

    [Builtin]
    public static BishString? ReadChar(BishReader self) => self.ReadChar();

    public BishString? ReadLine() => BishException.Wrapped(BishFileModule.Error, Reader.ReadLine) switch
    {
        null => null,
        var line => new BishString(line)
    };

    [Builtin]
    public static BishString? ReadLine(BishReader self) => self.ReadLine();

    [Builtin("hook")]
    public static BishFileCharIterator Get_chars(BishReader self) => new(self);

    [Builtin("hook")]
    public static BishFileLineIterator Get_lines(BishReader self) => new(self);

    [Builtin("hook")]
    public static BishString Get_content(BishReader self) =>
        new(BishException.Wrapped(BishFileModule.Error, self.Reader.ReadToEnd));
}

public class BishFileCharIterator(BishReader reader) : BishObject
{
    public BishReader Reader => reader;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("file.char.iter");

    [Iter]
    public BishString? Next() => Reader.ReadChar();
}

public class BishFileLineIterator(BishReader reader) : BishObject
{
    public BishReader Reader => reader;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("file.line.iter");

    [Iter]
    public BishString? Next() => Reader.ReadLine();
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
    public static void Write(BishWriter self, BishObject content) => self.Writer.Write(BishString.CallShow(content));
}