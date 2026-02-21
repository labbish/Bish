namespace BishRuntime;

[Flags]
public enum BishLookupMode
{
    None = 0,
    NoHook = 1 << 0,
    NotFromType = 1 << 1,
    NoBind = 1 << 2
}

public class BishObject(BishType? type = null)
{
    public virtual BishType DefaultType => StaticType;

    public static readonly BishType StaticType = new("object");

    public BishType Type
    {
        get => field ?? DefaultType;
        set;
    } = type;

    public readonly Dictionary<string, BishObject> Members = [];

    public BishObject? TryCallHook(string name, List<BishObject> args)
    {
        return TryGetMember(name, BishLookupMode.NoHook)?.TryCall(args);
    }

    public BishObject GetMember(string name, BishLookupMode mode = BishLookupMode.None) =>
        TryGetMember(name, mode) ?? throw BishException.OfAttribute("get", this, name);

    protected virtual List<BishObject> LookupChain => [];

    /**
     * Below is the lookup order. (It's messy and full of corner-cases, but works the most intuitive)
     * @GetFromType = (If not NotFromType mode) Recursively get on Type [NoHook, NotFromType, bind (if not NoBind mode)]
     * 1. Members of self
     * 2. (If this is a type) @GetFromType [ignore exceptions]
     * 3. (Only non-empty for types) Members on the lookup chain
     * 4. @GetFromType
     * 5. (If not NoHook mode) Call hook_Get
     *
     * ...And fun fact: Python used a simpler order, so that int.__str__(1) works but int.__str__() don't.
     * But we prefer class methods (e.g. obj.toString()) to free functions (e.g. str(obj)), so this works better here.
     */
    public BishObject? TryGetMember(string name, BishLookupMode mode = BishLookupMode.None,
        List<BishObject>? excludes = null)
    {
        excludes ??= [];
        if (excludes.Contains(this)) return null;

        BishObject? GetFromType() => mode.HasFlag(BishLookupMode.NotFromType)
            ? null
            : TryBind(Type.TryGetMember(name, mode | BishLookupMode.NoHook | BishLookupMode.NotFromType, excludes),
                mode.HasFlag(BishLookupMode.NoBind));

        // Step 1
        if (Members.TryGetValue(name, out var value)) return value;
        excludes.Add(this);

        // Step 2
        if (this is BishType)
        {
            var member = BishException.Ignored(GetFromType);
            if (member is not null) return member;
        }

        // Step 3
        foreach (var obj in LookupChain.Where(obj => !excludes.Contains(obj)))
        {
            if (obj.Members.TryGetValue(name, out var member)) return member;
            excludes.Add(obj);
        }

        // Step 4 & 5
        return GetFromType() ?? (mode.HasFlag(BishLookupMode.NoHook) ? null : TryCallGetHook(name));
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

    private BishObject? TryBind(BishObject? member, bool noBind) =>
        noBind ? member : member is BishFunc method ? method.Bind(this) : member;

    public BishObject SetMember(string name, BishObject value)
    {
        return TryCallHook("hook_Set", [new BishString(name), value]) ?? (Members[name] = value);
    }

    public BishObject DelMember(string name) =>
        TryDelMember(name) ?? throw BishException.OfAttribute("delete", this, name);

    public BishObject? TryDelMember(string name)
    {
        return Members.Remove(name, out var member) ? member : TryCallHook("hook_Del", [new BishString(name)]);
    }

    public BishObject Call(List<BishObject> args) => TryCall(args) ?? throw BishException.OfType_NotCallable(this);

    public virtual BishObject? TryCall(List<BishObject> args)
    {
        return TryGetMember("op_Call")?.TryCall(args);
    }

    public override string ToString()
    {
        return $"[Object {Type.Name}]";
    }

    [Builtin]
    public static BishString ToString(BishObject obj) => new(obj.ToString());

    [Builtin("op")]
    public static BishBool Eq(BishObject a, BishObject b) => new(a == b);

    public T ExpectToBe<T>(string expr) where T : BishObject => this switch
    {
        T t => t,
        { } result => throw BishException.OfType_Expect(expr, result, BishType.GetStaticType(typeof(T)))
    };

    [Builtin("op")]
    public static BishBool Neq(BishObject a, BishObject b) =>
        BishBool.Invert(BishOperator.Call("op_Eq", [a, b]).ExpectToBe<BishBool>($"{a} == {b}"));

    private static int Compare(BishObject a, BishObject b) =>
        BishOperator.Call("op_Cmp", [a, b]).ExpectToBe<BishInt>($"{a} <=> {b}").Value;

    [Builtin("op")]
    public static BishBool Lt(BishObject a, BishObject b) => new(Compare(a, b) < 0);

    [Builtin("op")]
    public static BishBool Le(BishObject a, BishObject b) => new(Compare(a, b) <= 0);

    [Builtin("op")]
    public static BishBool Gt(BishObject a, BishObject b) => new(Compare(a, b) > 0);

    [Builtin("op")]
    public static BishBool Ge(BishObject a, BishObject b) => new(Compare(a, b) >= 0);

    static BishObject() => BishBuiltinBinder.Bind<BishObject>();
}

public class BishType(string name, BishType[]? parents = null) : BishObject
{
    public readonly string Name = name;

    public BishType[] Parents
    {
        get => [..field, BishObject.StaticType];
    } = parents ?? [];

    protected override List<BishObject> LookupChain => LinearParents([]).Skip(1).ToList<BishObject>();

    public List<BishType> LinearParents(List<BishType> current)
    {
        if (current.Contains(this)) return [];
        List<BishType> results = [this];
        foreach (var parent in Parents)
            results.AddRange(parent.LinearParents(results));
        return results;
        // TODO: maybe an MRO
    }

    public BishObject CreateInstance(List<BishObject> args)
    {
        var instance = TryCallHook("hook_Create", []) ?? new BishObject(this);
        instance.Type = this; // TODO: do we really want this?
        instance.TryCallHook("hook_Init", args);
        return instance;
    }

    public override BishObject TryCall(List<BishObject> args) => CreateInstance(args);

    public bool CanAssignTo(BishType other, HashSet<BishObject>? excludes = null)
    {
        // Special case here
        if (this == BishInt.StaticType && other == BishNum.StaticType) return true;
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

    // TODO: maybe make it a getter (after we have it)
    [Builtin(special: false)]
    public static BishString GetName(BishType type) => new(type.Name);

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("type");

    internal static BishType GetStaticType(Type type) =>
        type.GetField("StaticType")?.GetValue(null) as BishType ??
        throw new ArgumentException($"Cannot find field `StaticType` on type {type}");

    static BishType()
    {
        BishBuiltinBinder.Bind<BishType>();
    }
}