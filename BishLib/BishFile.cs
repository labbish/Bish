using System.Text;
using BishRuntime;

namespace BishLib;

public static class BishFileModule
{
    public static BishObject Module => new BishObject
    {
        Members = new Dictionary<string, BishObject>
        {
            ["Reader"] = BishReader.StaticType,
        }
    };

    public static readonly BishType FileError = new("FileError", [BishError.StaticType]);

    public static BishException FileException(string msg) => BishException.Create(FileError, msg, []);

    public static T FileOperation<T>(Func<T> func)
    {
        try
        {
            return func();
        }
        catch (BishException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw FileException(e.Message);
        }
    }

    public static Encoding EncodingFrom(BishString? encoding) =>
        encoding is null ? Encoding.UTF8 : Encoding.GetEncoding(encoding.Value);
}

public class BishReader(StreamReader? reader) : BishObject
{
    public StreamReader Reader { get; private set; } = reader!;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Reader");

    [Builtin("hook")]
    public static BishReader Create(BishObject _) => new(null);

    [Builtin("hook")]
    public static void Init(BishReader self, BishString path, [DefaultNull] BishString? encoding) =>
        BishFileModule.FileOperation(() =>
            self.Reader = new StreamReader(path.Value, BishFileModule.EncodingFrom(encoding)));

    [Builtin("hook")]
    public static BishReader Enter(BishReader self) => self;

    [Builtin("hook")]
    public static void Exit(BishReader self, BishObject error) => self.Reader.Dispose();

    public BishString? ReadChar() => BishFileModule.FileOperation(Reader.Read) switch
    {
        -1 => null,
        var chr => new BishString((char)chr)
    };

    [Builtin(special: false)]
    public static BishObject ReadChar(BishReader self) => self.ReadChar() as BishObject ?? BishNull.Instance;

    public BishString? ReadLine() => BishFileModule.FileOperation(Reader.ReadLine) switch
    {
        null => null,
        var line => new BishString(line)
    };

    [Builtin(special: false)]
    public static BishObject ReadLine(BishReader self) => self.ReadLine() as BishObject ?? BishNull.Instance;

    [Builtin("hook")]
    public static BishFileCharIterator Get_chars(BishReader self) => new(self);

    [Builtin("hook")]
    public static BishFileLineIterator Get_lines(BishReader self) => new(self);

    [Builtin("hook")]
    public static BishString Get_content(BishReader self) => new(BishFileModule.FileOperation(self.Reader.ReadToEnd));

    static BishReader() => BishBuiltinBinder.Bind<BishReader>();
}

public class BishFileCharIterator(BishReader reader) : BishObject
{
    public BishReader Reader => reader;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("file.char.iter");

    [Iter]
    public BishString? Next() => Reader.ReadChar();

    static BishFileCharIterator() => BishBuiltinIteratorBinder.Bind<BishFileCharIterator>();
}

public class BishFileLineIterator(BishReader reader) : BishObject
{
    public BishReader Reader => reader;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("file.line.iter");

    [Iter]
    public BishString? Next() => Reader.ReadLine();

    static BishFileLineIterator() => BishBuiltinIteratorBinder.Bind<BishFileLineIterator>();
}

public class BishWriter(StreamWriter? writer) : BishObject
{
    public StreamWriter Writer { get; private set; } = writer!;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Writer");

    [Builtin("hook")]
    public static BishWriter Create(BishObject _) => new(null);

    [Builtin("hook")]
    public static void Init(BishWriter self, BishString path, [DefaultNull] BishBool? append,
        [DefaultNull] BishString? encoding) =>
        BishFileModule.FileOperation(() =>
            self.Writer = new StreamWriter(path.Value, append: append?.Value ?? false,
                BishFileModule.EncodingFrom(encoding)));

    [Builtin("hook")]
    public static BishWriter Enter(BishWriter self) => self;

    [Builtin("hook")]
    public static void Exit(BishWriter self, BishObject error) => self.Writer.Dispose();

    [Builtin(special: false)]
    public static void Write(BishWriter self, BishObject content) =>
        self.Writer.Write(BishString.CallToString(content));

    static BishWriter() => BishBuiltinBinder.Bind<BishWriter>();
}