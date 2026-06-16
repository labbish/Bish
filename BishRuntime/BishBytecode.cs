using System.Diagnostics.CodeAnalysis;

namespace BishRuntime;

[AttributeUsage(AttributeTargets.Class)]
public class BytecodeAttribute : Attribute;

// Should be replaced with a union in C#15.
public class Tag(string? s, byte b = 0)
{
    internal string? S => s;
    internal byte B => b;

    public static bool operator ==(Tag? self, Tag? other)
    {
        if (self is null && other is null) return true;
        if (self is null || other is null) return false;
        if (self.S is null && other.S is null) return self.B == other.B;
        return self.S == other.S;
    }

    public static bool operator !=(Tag? self, Tag? other) => !(self == other);

    [return: NotNullIfNotNull(nameof(s))]
    public static implicit operator Tag?(string? s) => s is null ? null : new Tag(s);

    public static implicit operator Tag(byte b) => new(null, b);

    public override bool Equals(object? obj) => obj is Tag tag && this == tag;

    public override int GetHashCode() => S is null ? B.GetHashCode() : S.GetHashCode();

    public override string ToString() => S ?? B.ToString();
}

public abstract record BishBytecode
{
    public Tag? Tag;
    public SourcePosition? Pos;

    public abstract void Execute(BishFrame frame);

    public BishBytecode Tagged(Tag? tag)
    {
        Tag = tag;
        return this;
    }

    public BishBytecode WithPos(SourcePosition? position)
    {
        Pos = position;
        return this;
    }

    public BishBytecode Stripped() => this with { Pos = null };
}

public class BishBytecodeObject : BishObject
{
    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("Bytecode");
    public BishBytecode? Bytecode;

    [Builtin("hook")]
    public static BishBytecodeObject New(BishString type, BishObject obj)
    {
        var self = new BishBytecodeObject();
        self.AddString("type", type.Value);
        foreach (var (name, value) in obj.Vars) self.DefMember(name, value);
        self.Bytecode = BishBytecodeParser.FromObject(self);
        return self;
    }

    [Builtin]
    public static BishBytecodeObject Tagged(BishBytecodeObject self, BishString tag)
    {
        self.AddString("tag", tag.Value);
        return self;
    }

    [Builtin("op")]
    public static BishBool Eq(BishBytecodeObject self, BishBytecodeObject other) =>
        BishBool.Of(BishBytecodeParser.FromObject(self).Stripped() == BishBytecodeParser.FromObject(other).Stripped());

    public void AddInt(string name, int value) => DefMember(name, BishInt.Of(value));

    public void AddDouble(string name, double value) => DefMember(name, new BishNum(value));

    public void AddBool(string name, bool value) => DefMember(name, BishBool.Of(value));

    public void AddString(string name, string value) => DefMember(name, new BishString(value));

    public void AddTag(string name, Tag? value) => DefMember(name, value switch
    {
        null => BishNull.Instance,
        { S: null, B: var b } => BishInt.Of(b),
        { S: var s } => new BishString(s)
    });

    public void AddStrings(string name, IList<string> value) => DefMember(name,
        new BishList(value.Select(item => new BishString(item)).ToList<BishObject>()));

    public void AddPosition(string name, SourcePosition? position) =>
        DefMember(name, position is null ? BishNull.Instance : position.ToObject());

    public T1 Get<T, T1>(string name, Func<T, T1> process, T1? defaultValue = default) where T : BishObject
    {
        var value = TryGetMember(name)?.As<T>(name);
        return value is not null ? process(value) : defaultValue ?? throw BishException.OfAttribute("get", this, name);
    }

    public int GetInt(string name, int? defaultValue = null) =>
        Get(name, (BishInt value) => value.Value, defaultValue)!.Value;

    public double GetDouble(string name, double? defaultValue = null) =>
        Get(name, (BishNum value) => value.Value, defaultValue)!.Value;

    public bool GetBool(string name, bool? defaultValue = null) =>
        Get(name, (BishBool value) => value.Value, defaultValue)!.Value;

    public string GetString(string name, string? defaultValue = null) =>
        Get(name, (BishString value) => value.Value, defaultValue);

    public Tag? GetTag(string name) => TryGetMember(name) switch
    {
        null or BishNull => null,
        BishInt i => (byte)i.Value,
        BishString s => s.Value,
        var x => throw BishException.OfType_Expect(name, x, "null or int or string")
    };

    public SourcePosition? GetPos(string name) => TryGetMember(name) switch
    {
        null or BishNull => null,
        BishList list => new SourcePosition(
            list.Index(0).As<BishInt>("line").Value, list.Index(1).As<BishInt>("column").Value,
            list.Index(2).As<BishInt>("stopLine").Value, list.Index(3).As<BishInt>("stopColumn").Value),
        var x => throw BishException.OfType_Expect(name, x, "null or list[int]")
    };

    public string[] GetStrings(string name) => GetMember(name).As<BishList>("list").List
        .Select(item => item.As<BishString>("list item").Value).ToArray();

    [Builtin]
    public static BishString Repr(BishBytecodeObject self, BishReprContext ctx)
    {
        var repr = self.Bytecode is null ? "ERROR" : BishBytecodeParser.ToString(self.Bytecode);
        return new BishString(ctx.Debug ? $"bytecode({repr})" : repr);
    }
}