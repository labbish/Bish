using System.Diagnostics.CodeAnalysis;

namespace BishRuntime;

[Flags]
public enum BishLookupMode
{
    None = 0,
    NoHook = 1 << 0,
    NotFromType = 1 << 1,
    NoBind = 1 << 2,
    NoGetter = 1 << 3
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

    public Dictionary<string, BishObject> Members = [];

    public BishObject? TryCallHook(string name, List<BishObject> args) =>
        TryGetMember(name, BishLookupMode.NoHook | BishLookupMode.NoGetter)?.TryCall(args);

    public BishObject GetMember(string name, BishLookupMode mode = BishLookupMode.None) =>
        TryGetMember(name, mode) ?? throw BishException.OfAttribute("get", this, name);

    protected virtual List<BishObject> LookupChain => [this];

    /**
     * Below is the lookup order. (It's messy and full of corner-cases, but works the most intuitive)
     * @GetFromType = (If not NotFromType mode) Recursively get on Type [NoHook, NotFromType, bind (if not NoBind mode)]
     * 1. Members (including getter) of first of lookup chain
     * 2. (If this is a type) @GetFromType [ignore exceptions]
     * 3. (Only non-empty for types) Members (including getter) of the rest of the lookup chain
     * 4. @GetFromType
     * 5. (If not NoHook mode) Call hook_Get
     *
     * ...And fun fact: Python used a simpler order, so that int.__str__(1) works but int.__str__() don't.
     * But we prefer class methods (e.g. obj.toString()) to free functions (e.g. str(obj)), so this works better here.
     */
    public virtual BishObject? TryGetMember(string name, BishLookupMode mode = BishLookupMode.None,
        BishType? mroRoot = null, List<BishObject>? excludes = null)
    {
        excludes ??= [];
        mroRoot ??= Type;
        if (excludes.Contains(this)) return null;

        var chain = LookupChain;
        var first = chain.ElementAtOrDefault(0);
        chain = chain.Skip(1).ToList();

        // Step 1
        if (TryGetFromMember(first, out var result)) return result;

        // Step 2
        if (this is BishType)
        {
            var member = BishException.Ignored(GetFromType);
            if (member is not null) return member;
        }

        // Step 3
        foreach (var obj in chain.Where(obj => !excludes.Contains(obj)))
            if (TryGetFromMember(obj, out var member))
                return member;

        // Step 4 & 5
        return GetFromType() ?? (mode.HasFlag(BishLookupMode.NoHook) ? null : TryCallGetHook(name));

        BishObject? GetFromType() => mode.HasFlag(BishLookupMode.NotFromType)
            ? null
            : TryBind(
                Type.WithMRORoot(mroRoot).TryGetMember(name, mode | BishLookupMode.NoHook | BishLookupMode.NotFromType,
                    excludes: excludes), mode.HasFlag(BishLookupMode.NoBind));

        bool TryGetFromMember(BishObject? obj, [NotNullWhen(true)] out BishObject? member)
        {
            member = null;
            if (obj is null) return false;
            excludes.Add(obj);
            member = obj.Members.GetValueOrDefault(name) ??
                     (mode.HasFlag(BishLookupMode.NoGetter) ? null : obj.TryCallHook($"hook_Get_{name}", []));
            return member is not null;
        }
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

    public BishObject SetMember(string name, BishObject value) =>
        TryCallHook("hook_Set", [new BishString(name), value]) ??
        TryCallHook($"hook_Set_{name}", [value]) ?? (Members[name] = value);

    public BishObject DelMember(string name) =>
        TryDelMember(name) ?? throw BishException.OfAttribute("delete", this, name);

    public BishObject? TryDelMember(string name) =>
        TryCallHook($"hook_Del_{name}", []) ?? (Members.Remove(name, out var member)
            ? member
            : TryCallHook("hook_Del", [new BishString(name)]));

    public BishObject Call(List<BishObject> args) => TryCall(args) ?? throw BishException.OfType_NotCallable(this);

    public virtual BishObject? TryCall(List<BishObject> args) => TryGetMember("op_Call")?.TryCall(args);

    public override string ToString() => $"[Object {Type.Name}]";

    [Builtin]
    public static BishString ToString(BishObject obj) => new(obj.ToString());

    [Builtin("op")]
    public static BishBool Eq(BishObject a, BishObject b) => new(a == b);

    public T ExpectToBe<T>(string expr) where T : BishObject => this switch
    {
        T t => t,
        { } result => throw BishException.OfType_Expect(expr, result, BishType.GetStaticType(typeof(T)))
    };

    public BishObject? TryConvert(BishType type)
    {
        // Special case
        if (Type == BishInt.StaticType && type == BishNum.StaticType) return BishNum.StaticType.CreateInstance([this]);
        return Type.CanAssignTo(type) ? this : null;
    }

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

    // Cannot write `[DefaultNull] BishType? root` because it causes cycle reference in static initialization
    [Builtin(special: false)]
    public static BishBaseObject Base(BishObject self, [DefaultNull] BishObject? root) =>
        new(self,
            root?.ExpectToBe<BishType>("root") ??
            self.Type.LookupChain.ElementAtOrDefault(1)?.ExpectToBe<BishType>("base type") ??
            throw BishException.OfType_NoBase(self));

    static BishObject() => BishBuiltinBinder.Bind<BishObject>();
}

public partial class BishType(string name, List<BishType>? parents = null, int skips = 0) : BishObject
{
    public readonly string Name = name;
    public readonly List<BishType> Parents = parents ?? [];
    public readonly int Skips = skips;

    protected override List<BishObject> LookupChain =>
        GetMRO().Concat([BishObject.StaticType]).Skip(Skips).ToList<BishObject>();

    public BishObject CreateInstance(List<BishObject> args)
    {
        var instance = TryCallHook("hook_Create", []) ?? new BishObject();
        instance.Type = this; // We need this when hook_Create is called on the parent class
        instance.TryCallHook("hook_Init", args);
        return instance;
    }

    public override BishObject TryCall(List<BishObject> args) => CreateInstance(args);

    public bool CanAssignTo(BishType other)
    {
        // Special case here
        if (this == BishInt.StaticType && other == BishNum.StaticType) return true;
        return LookupChain.Contains(other);
    }

    public override string ToString() => Name;

    [Builtin("hook")]
    public static BishString Get_name(BishType type) => new(type.Name);

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("type");

    internal static BishType GetStaticType(Type type) =>
        type.GetField("StaticType")?.GetValue(null) as BishType ??
        throw new ArgumentException($"Cannot find field `StaticType` on type {type}");

    public BishType WithMRORoot(BishType mroRoot) =>
        new(Name, Parents, GetMRO().Concat([BishObject.StaticType]).ToList().FindIndex(type => type == mroRoot))
            { Members = Members };

    static BishType() => BishBuiltinBinder.Bind<BishType>();
}