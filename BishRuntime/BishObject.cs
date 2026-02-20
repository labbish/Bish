namespace BishRuntime;

[Flags]
public enum BishLookupMode
{
    None = 0,
    NoHook = 1
}

public class BishObject(BishType? type = null)
{
    public virtual BishType DefaultType => StaticType;

    public static readonly BishType StaticType = new("object");

    public BishType Type
    {
        get => field ?? DefaultType;
    } = type;

    public readonly Dictionary<string, BishObject> Members = [];

    public BishObject? TryCallHook(string name, List<BishObject> args)
    {
        return TryGetMember(name, BishLookupMode.NoHook)?.TryCall(args);
    }

    public BishObject GetMember(string name, BishLookupMode mode = BishLookupMode.None) =>
        TryGetMember(name, mode) ?? throw new BishNoSuchMemberException(this, name);

    protected virtual List<BishObject> LookupChain => [this];

    public BishObject? TryGetMember(string name, BishLookupMode mode = BishLookupMode.None,
        List<BishObject>? excludes = null)
    {
        excludes ??= [];
        if (excludes.Contains(this)) return null;
        foreach (var obj in LookupChain.Where(obj => !excludes.Contains(obj)))
        {
            if (obj.Members.TryGetValue(name, out var member)) return member;
            excludes.Add(obj);
        }

        return TryBind(Type.TryGetMember(name, mode | BishLookupMode.NoHook, excludes)) ??
               (mode.HasFlag(BishLookupMode.NoHook) ? null : TryCallGetHook(name));
    }

    private BishObject? TryCallGetHook(string name, List<BishObject>? excludes = null)
    {
        excludes ??= [];
        if (excludes.Contains(this)) return null;
        var result = TryCallHook("hook_Get", [new BishString(name)]);
        if (result is not null) return result;
        excludes.Add(this);
        return Type.TryCallGetHook(name, excludes);
    }

    private BishObject? TryBind(BishObject? member) => member is BishMethod method ? method.Bind(this) : member;

    public BishObject SetMember(string name, BishObject value)
    {
        return TryCallHook("hook_Set", [new BishString(name), value]) ?? (Members[name] = value);
    }

    public BishObject DelMember(string name) => TryDelMember(name) ?? throw new BishNoSuchMemberException(this, name);

    public BishObject? TryDelMember(string name)
    {
        return Members.Remove(name, out var member) ? member : TryCallHook("hook_Del", [new BishString(name)]);
    }

    public BishObject Call(List<BishObject> args) => TryCall(args) ?? throw new BishNotCallableException(this);

    public virtual BishObject? TryCall(List<BishObject> args)
    {
        return TryGetMember("op_Call")?.TryCall(args);
    }

    public override string ToString()
    {
        return $"[Object {Type.Name}]";
    }

    [Builtin]
    public static BishString ToString(BishObject obj)
    {
        return new BishString(obj.ToString());
    }

    static BishObject() => BuiltinBinder.Bind<BishObject>();
}

public class BishType(string name, BishType[]? parents = null) : BishObject
{
    public readonly string Name = name;

    public BishType[] Parents
    {
        get => [..field, BishObject.StaticType];
    } = parents ?? [];

    protected override List<BishObject> LookupChain => LinearParents([]).ToList<BishObject>();

    public List<BishType> LinearParents(List<BishType> current)
    {
        if (current.Contains(this)) return [];
        List<BishType> results = [this];
        foreach (var parent in Parents)
            results.AddRange(parent.LinearParents(results));
        return results;
    }

    public BishObject CreateInstance(List<BishObject> args)
    {
        var instance = TryCallHook("hook_Create", []) ?? new BishObject(this);
        instance.TryCallHook("hook_Init", args);
        return instance;
    }

    public bool CanAssignTo(BishType other, HashSet<BishObject>? excludes = null)
    {
        excludes ??= [];
        if (excludes.Contains(this)) return false;
        if (this == other) return true;
        excludes.Add(this);
        return Parents.Any(parent => parent.CanAssignTo(other, excludes));
    }

    public override string ToString()
    {
        return $"[Type {Name}]";
    }

    [Builtin]
    public static BishString GetName(BishType type) => new(type.Name);

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("type");
    
    static BishType() => BuiltinBinder.Bind<BishType>();
}