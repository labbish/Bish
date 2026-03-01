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
        internal set;
    } = type;

    public Dictionary<string, BishObject> Members = [];

    public BishObject? TryCallHook(string name, List<BishObject> args, bool ignores = false)
    {
        var hook = TryGetMember(name, BishLookupMode.NoHook | BishLookupMode.NoGetter);
        if (ignores && hook is BishFunc { Tag: "ignore" }) return null;
        return hook?.TryCall(args);
    }

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
     * 5. (If not NoHook mode) Call hook_get
     *
     * ...And fun fact: Python used a simpler order, so that int.__str__(1) works but int.__str__() don't.
     * But we prefer class methods (e.g. obj.toString()) to free functions (e.g. str(obj)), so this works better here.
     */
    public virtual BishObject? TryGetMember(string name, BishLookupMode mode = BishLookupMode.None,
        BishType? mroRoot = null, List<BishObject>? excludes = null, BishObject? boundSelf = null)
    {
        var self = mroRoot is null ? this : Base(this, mroRoot);
        excludes ??= [];
        mroRoot ??= Type;
        boundSelf ??= this;
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
        return GetFromType() ?? (mode.HasFlag(BishLookupMode.NoHook) ? null : self.TryCallGetHook(name));

        BishObject? GetFromType() => mode.HasFlag(BishLookupMode.NotFromType)
            ? null
            : TryBind(
                Type.WithMRORoot(mroRoot).TryGetMember(name, mode | BishLookupMode.NoHook | BishLookupMode.NotFromType,
                    excludes: excludes), mode.HasFlag(BishLookupMode.NoBind));

        BishObject? TryBind(BishObject? member, bool noBind) =>
            !noBind && member is BishFunc method ? method.Bind(boundSelf) : member;

        bool TryGetFromMember(BishObject? obj, [NotNullWhen(true)] out BishObject? member)
        {
            member = null;
            if (obj is null) return false;
            excludes.Add(obj);
            member = obj.Members.GetValueOrDefault(name) ??
                     (mode.HasFlag(BishLookupMode.NoGetter) ? null : obj.TryCallHook($"hook_get_{name}", []));
            return member is not null;
        }
    }

    private BishObject? TryCallGetHook(string name, List<BishObject>? excludes = null)
    {
        excludes ??= [];
        if (excludes.Contains(this)) return null;
        var result = TryCallHook("hook_get", [new BishString(name)], ignores: true);
        if (result is not null) return result;
        excludes.Add(this);
        return Type.TryCallGetHook(name, excludes);
    }

    [Builtin("hook", tag: "ignore")]
    public static BishObject Get(BishObject self, BishString name) => self.GetMember(name.Value, BishLookupMode.NoHook);

    public virtual BishObject SetMember(string name, BishObject value, BishObject? mroRoot = null)
    {
        var self = mroRoot is null ? this : Base(this, mroRoot);
        var hooked = this is BishType
            ? null
            : self.TryCallHook("hook_set", [new BishString(name), value], ignores: true);
        return hooked ?? self.TryCallHook($"hook_set_{name}", [value]) ?? (Members[name] = value);
    }

    [Builtin("hook", tag: "ignore")]
    public static BishObject Set(BishObject self, BishString name, BishObject value) =>
        self.SetMember(name.Value, value);

    public BishObject DelMember(string name) =>
        TryDelMember(name) ?? throw BishException.OfAttribute("delete", this, name);

    public virtual BishObject? TryDelMember(string name, BishObject? mroRoot = null)
    {
        var self = mroRoot is null ? this : Base(this, mroRoot);
        return self.TryCallHook($"hook_del_{name}", []) ?? (Members.Remove(name, out var member)
            ? member
            : self.TryCallHook("hook_del", [new BishString(name)], ignores: true));
    }

    [Builtin("hook", tag: "ignore")]
    public static BishObject Del(BishObject self, BishString name) => self.DelMember(name.Value);

    public BishObject Call(List<BishObject> args) => TryCall(args) ?? throw BishException.OfType_NotCallable(this);

    public virtual BishObject? TryCall(List<BishObject> args) => TryGetMember("op_call")?.TryCall(args);

    public override string ToString() => $"[Object {Type.Name}]";

    [Builtin]
    public static BishString ToString(BishObject obj) => new(obj.ToString());

    [Builtin("op")]
    public static BishBool Eq(BishObject a, BishObject b) => BishBool.Of(a == b);

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
        BishBool.Invert(BishOperator.Call("op_eq", [a, b]).ExpectToBe<BishBool>($"{a} == {b}"));

    private static int Compare(BishObject a, BishObject b) =>
        BishOperator.Call("op_cmp", [a, b]).ExpectToBe<BishInt>($"{a} <=> {b}").Value;

    [Builtin("op")]
    public static BishBool Lt(BishObject a, BishObject b) => BishBool.Of(Compare(a, b) < 0);

    [Builtin("op")]
    public static BishBool Le(BishObject a, BishObject b) => BishBool.Of(Compare(a, b) <= 0);

    [Builtin("op")]
    public static BishBool Gt(BishObject a, BishObject b) => BishBool.Of(Compare(a, b) > 0);

    [Builtin("op")]
    public static BishBool Ge(BishObject a, BishObject b) => BishBool.Of(Compare(a, b) >= 0);

    // Cannot write `[DefaultNull] BishType? root` because it causes cycle reference in static initialization
    [Builtin(special: false)]
    public static BishBaseObject Base(BishObject self, [DefaultNull] BishObject? root) =>
        new(self,
            root?.ExpectToBe<BishType>("root") ??
            self.Type.LookupChain.ElementAtOrDefault(1)?.ExpectToBe<BishType>("base type") ??
            throw BishException.OfType_NoBase(self));

    public virtual BishReflect Reflect() => new(this);

    static BishObject() => BishBuiltinBinder.Bind<BishObject>();
}

public class BishReflect(BishObject obj) : BishObject
{
    public BishObject Object => obj;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("reflect");

    [Builtin("hook")]
    public static BishObject Get_object(BishReflect self) => self.Object;

    [Builtin("hook")]
    public static BishProxyMap Get_members(BishReflect self) => new(self.Object.Members);

    [Builtin("hook")]
    public static BishType Get_type(BishReflect self) => self.Object.Type;

    [Builtin("hook")]
    public static BishType Set_type(BishReflect self, BishType type) => self.Object.Type = type;

    public override string ToString() => $"[{Object} reflect]";

    static BishReflect() => BishBuiltinBinder.Bind<BishReflect>();
}