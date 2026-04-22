using BishUtils;

namespace BishRuntime;

public class BishBytecodeWriter(BinaryWriter writer)
{
    public void AddByte(byte value) => writer.Write(value);
    public void AddBytes(byte[] value) => writer.Write(value);
    public void AddInt(int value) => AddBytes(BitConverter.GetBytes(value));
    public void AddDouble(double value) => AddBytes(BitConverter.GetBytes(value));
    public void AddBool(bool value) => AddByte(value ? (byte)1 : (byte)0);

    public void AddString(string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        AddInt(bytes.Length);
        AddBytes(bytes);
    }

    public void AddStringN(string? value)
    {
        if (value is null) AddInt(int.MinValue);
        else AddString(value);
    }

    public void AddStrings(IList<string> value)
    {
        AddInt(value.Count);
        foreach (var str in value) AddString(str);
    }
}

public class BishBytecodeReader(BinaryReader reader)
{
    public byte GetByte() => reader.ReadByte();

    public byte[] GetBytes(int count)
    {
        var buffer = new byte[count];
        reader.ReadExactly(buffer);
        return buffer;
    }

    public int GetInt() => BitConverter.ToInt32(GetBytes(4));

    public double GetDouble() => BitConverter.ToDouble(GetBytes(8));
    public bool GetBool() => GetByte() != 0;

    public string GetString(int length) => System.Text.Encoding.UTF8.GetString(GetBytes(length));

    public string GetString() => GetString(GetInt());

    public string? GetStringN()
    {
        var length = GetInt();
        return length == int.MinValue ? null : GetString(length);
    }

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
    public const byte Version = 0;

    public static readonly IList<BytecodeParser> Parsers = new ConcurrentList<BytecodeParser>();

    public static string ToString(BishBytecode bytecode)
    {
        var parser = Parsers.FirstOrDefault(p => p.Type == bytecode.GetType()) ??
                     throw new ArgumentException($"Invalid Bytecode: {bytecode.GetType().Name}");
        return (bytecode.Tag is null ? "" : bytecode.Tag + ": ") + parser.Format(bytecode);
    }

    extension(BishBytecodeWriter writer)
    {
        public void WriteSingle(BishBytecode bytecode)
        {
            var index = Parsers.FindIndex(p => p.Type == bytecode.GetType());
            if (index == -1) throw new ArgumentException($"Invalid Bytecode: {bytecode.GetType().Name}");
            writer.AddByte((byte)index);
            writer.AddStringN(bytecode.Tag);
            var parser = Parsers[index];
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
            var tag = reader.GetStringN();
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
        public void WriteBytecodes(IEnumerable<BishBytecode> bytecodes) =>
            new BishBytecodeWriter(new BinaryWriter(stream)).Write(bytecodes);

        public IEnumerable<BishBytecode> ReadBytecodes() =>
            new BishBytecodeReader(new BinaryReader(stream)).Read();
    }
}

public record BytecodeParser(
    Type Type,
    Func<BishBytecode, string> Format,
    Action<BishBytecode, BishBytecodeWriter> Write,
    Func<BishBytecodeReader, BishBytecode> Read);