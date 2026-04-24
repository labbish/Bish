using System.Buffers.Binary;
using System.Text;
using BishUtils;

namespace BishRuntime;

// Note: length of string is always >=0, so it is fine to make use of 0x80~0xff
public static class StringTags
{
    public const byte NullTag = 0xff;
    public const byte ByteTag = 0xfe;
    public const byte Repeated = 0xee;
}

public class BishBytecodeWriter(BinaryWriter writer)
{
    private readonly List<string> _strings = [];

    public void AddByte(byte value) => writer.Write(value);
    public void AddBytes(Span<byte> value) => writer.Write(value);

    public void AddInt(int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        AddBytes(bytes);
    }

    public void AddDouble(double value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleBigEndian(bytes, value);
        AddBytes(bytes);
    }

    public void AddBool(bool value) => AddByte(value ? (byte)1 : (byte)0);

    public void AddString(string value)
    {
        var index = _strings.IndexOf(value);
        if (index != -1)
        {
            AddByte(StringTags.Repeated);
            AddInt(index);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        AddInt(bytes.Length);
        _strings.Add(value);
        AddBytes(bytes);
    }

    public void AddTag(Tag? value)
    {
        if (value is null) AddByte(StringTags.NullTag);
        else if (value.S is null)
        {
            AddByte(StringTags.ByteTag);
            AddByte(value.B);
        }
        else AddString(value.S);
    }

    public void AddStrings(IList<string> value)
    {
        AddInt(value.Count);
        foreach (var str in value) AddString(str);
    }
}

public class BishBytecodeReader(BinaryReader reader)
{
    private readonly List<string> _strings = [];

    public byte GetByte() => reader.ReadByte();

    protected T ProcessBytes<T>(int count, Func<ReadOnlySpan<byte>, T> processor)
    {
        Span<byte> buffer = stackalloc byte[count];
        reader.ReadExactly(buffer);
        return processor(buffer);
    }

    public int GetInt() => ProcessBytes(4, BinaryPrimitives.ReadInt32BigEndian);
    public double GetDouble() => ProcessBytes(8, BinaryPrimitives.ReadDoubleBigEndian);
    public bool GetBool() => GetByte() != 0;

    public string GetString(byte first)
    {
        if (first == StringTags.Repeated) return _strings[GetInt()];
        Span<byte> rest = stackalloc byte[3];
        reader.ReadExactly(rest);
        var length = BinaryPrimitives.ReadInt32BigEndian([first, ..rest]);
        var result = ProcessBytes(length, Encoding.UTF8.GetString);
        _strings.Add(result);
        return result;
    }

    public string GetString() => GetString(GetByte());

    public Tag? GetTag() => GetByte() switch
    {
        0xff => null,
        0xfe => GetByte(),
        var first => GetString(first)
    };

    public string[] GetStrings()
    {
        List<string> list = [];
        var length = GetInt();
        for (var i = 0; i < length; i++) list.Add(GetString());
        return list.ToArray();
    }

    public bool IsEmpty() => reader.BaseStream.Position >= reader.BaseStream.Length;
}

public static class BishBytecodeParser
{
    public const int Magic = 0x0d000721;
    public const byte Version = 2;

    public static readonly IList<BytecodeParser> Parsers = new ConcurrentList<BytecodeParser>();

    private static (BytecodeParser Parser, byte Index) GetParser(BishBytecode bytecode)
    {
        var index = Parsers.FindIndex(p => p.Type == bytecode.GetType());
        return index == -1
            ? throw new ArgumentException($"Invalid Bytecode: {bytecode.GetType().Name}")
            : (Parsers[index], (byte)index);
    }

    public static string ToString(BishBytecode bytecode)
    {
        var (parser, _) = GetParser(bytecode);
        return (bytecode.Tag is null ? "" : bytecode.Tag + ": ") + parser.Format(bytecode);
    }

    extension(BishBytecodeWriter writer)
    {
        public void WriteSingle(BishBytecode bytecode)
        {
            var (parser, index) = GetParser(bytecode);
            writer.AddByte(index);
            writer.AddTag(bytecode.Tag);
            parser.Write(bytecode, writer);
        }

        public void Write(IEnumerable<BishBytecode> bytecodes)
        {
            writer.AddInt(Magic);
            writer.AddByte(Version);
            foreach (var bytecode in bytecodes)
                writer.WriteSingle(bytecode);
        }
    }

    extension(BishBytecodeReader reader)
    {
        public BishBytecode ReadSingle()
        {
            var index = reader.GetByte();
            var parser = Parsers.ElementAtOrDefault(index) ??
                         throw new ArgumentException($"Invalid Bytecode Index: {index}");
            var tag = reader.GetTag();
            return parser.Read(reader).Tagged(tag);
        }

        public IEnumerable<BishBytecode> Read()
        {
            if (reader.GetInt() != Magic) throw new ArgumentException("Bad Bytecode Magic Number!");
            var version = reader.GetByte();
            if (version != Version) throw new ArgumentException($"Bad Bytecode Version {version}; expected {Version}!");
            while (!reader.IsEmpty()) yield return reader.ReadSingle();
        }
    }

    extension(Stream stream)
    {
        public void WriteBytecodes(IEnumerable<BishBytecode> bytecodes)
        {
            using var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            new BishBytecodeWriter(bw).Write(bytecodes);
        }

        public BishBytecode[] ReadBytecodes()
        {
            using var br = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            return new BishBytecodeReader(br).Read().ToArray();
        }
    }

    public static BishBytecodeObject ToObject(BishBytecode bytecode)
    {
        var result = new BishBytecodeObject();
        GetParser(bytecode).Parser.WriteObject(bytecode, result);
        result.AddString("type", bytecode.GetType().Name);
        result.AddTag("tag", bytecode.Tag);
        return result;
    }

    public static BishBytecode FromObject(BishBytecodeObject bytecode)
    {
        var type = bytecode.GetString("type");
        var index = Parsers.FindIndex(parser => parser.Type.Name == type);
        if (index == -1) throw new ArgumentException($"Invalid Bytecode: {type}");
        var tag = bytecode.GetTag("tag");
        return Parsers[index].ReadObject(bytecode).Tagged(tag);
    }
}

public record BytecodeParser(
    Type Type,
    Func<BishBytecode, string> Format,
    Action<BishBytecode, BishBytecodeWriter> Write,
    Func<BishBytecodeReader, BishBytecode> Read,
    Action<BishBytecode, BishBytecodeObject> WriteObject,
    Func<BishBytecodeObject, BishBytecode> ReadObject);