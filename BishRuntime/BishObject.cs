using System.Collections.Concurrent;
using BishUtils;

namespace BishRuntime;

[Flags]
public enum BishLookupMode
{
    None = 0,
    NoHook = 1 << 0,
    NotFromType = 1 << 1,
    NoBind = 1 << 2,
    NoAccessor = 1 << 3
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

    public IDictionary<string, BishObject> Vars = new ConcurrentDictionary<string, BishObject>();

    protected virtual IList<BishObject> LookupChain => (ConcurrentList<BishObject>)[this];

    protected virtual BishType MRORoot => Type;
    protected virtual BishObject BoundThis => this;

    public BishObject? TryCallHook(string name, BishArgs args, bool ignores = false)
    {
        var hook = TryGetMember(name, BishLookupMode.NoHook | BishLookupMode.NoAccessor);
        if (ignores && hook is BishFunc { Tag: "ignore" }) return null;
        return hook?.TryCall(args);
    }

    public BishVarHandle? TryGetHandle(string name, string op, BishLookupMode mode = BishLookupMode.None,
        IList<BishObject>? excludes = null)
    {
        excludes ??= [];
        if (excludes.Contains(this)) return null;

        foreach (var obj in LookupChain.Where(obj => !excludes.Contains(obj)))
        {
            excludes.Add(obj);
            if (obj.Vars.ContainsKey(name)) return new BishVarHandle(obj, name, BishVarHandleType.Normal);
            if (mode.HasFlag(BishLookupMode.NoAccessor)) continue;
            var accessor = obj.TryGetHandle($"hook_{op}_{name}", "get",
                BishLookupMode.NoHook | BishLookupMode.NoAccessor);
            if (accessor is not null) return new BishVarHandle(accessor.Owner, name, BishVarHandleType.Accessor);
        }

        if (!mode.HasFlag(BishLookupMode.NotFromType))
        {
            var type = Type.WithMRORoot(MRORoot).TryGetHandle(name, op,
                mode | BishLookupMode.NoHook | BishLookupMode.NotFromType, excludes: excludes);
            if (type is not null) return type with { Bind = !mode.HasFlag(BishLookupMode.NoBind) };
        }

        if (mode.HasFlag(BishLookupMode.NoHook)) return null;
        var hook = Base(this, MRORoot).TryGetHookHandle(op);
        return hook is null ? null : hook with { Type = BishVarHandleType.Hook };
    }

    private BishVarHandle? TryGetHookHandle(string op, IList<BishObject>? excludes = null)
    {
        excludes ??= [];
        if (excludes.Contains(this)) return null;
        var result = TryGetHandle($"hook_{op}", "get", BishLookupMode.NoHook | BishLookupMode.NoAccessor);
        if (result is not null && result.Owner != StaticType) return result;
        excludes.Add(this);
        return Type.TryGetHookHandle(op, excludes);
    }

    public BishObject GetMember(string name, BishLookupMode mode = BishLookupMode.None) =>
        TryGetMember(name, mode) ?? throw BishException.OfAttribute("get", this, name);

    public BishObject? TryGetMember(string name, BishLookupMode mode = BishLookupMode.None)
    {
        var handle = TryGetHandle(name, "get", mode);
        if (handle is null) return null;
        var result = handle.Type switch
        {
            BishVarHandleType.Normal => handle.Owner.Vars[handle.Name],
            BishVarHandleType.Hook => handle.Owner.Vars["hook_get"]
                .Call(new BishArgs([this, new BishString(handle.Name)])),
            BishVarHandleType.Accessor => handle.Owner.Vars[$"hook_get_{handle.Name}"].Call(new BishArgs([this])),
            _ => throw new ArgumentException("impossible!")
        };
        return handle.Bind ? result.Bind(BoundThis) : result;
    }

    public virtual BishObject Bind(BishObject self) =>
        TryCallHook("hook_bind", new BishArgs([self]), ignores: true) ?? this;

    [Builtin("hook", tag: "ignore")]
    public static BishObject Get(BishObject self, BishString name) => self.GetMember(name.Value, BishLookupMode.NoHook);

    public BishObject? TrySetMember(string name, BishObject value, BishLookupMode mode = BishLookupMode.None)
    {
        var handle = TryGetHandle(name, "set", mode);
        if (handle is null) return null;
        return handle.Type switch
        {
            BishVarHandleType.Normal => handle.Owner.Vars[handle.Name] = value,
            BishVarHandleType.Hook => handle.Owner.Vars["hook_set"]
                .Call(new BishArgs([this, new BishString(handle.Name), value])),
            BishVarHandleType.Accessor => handle.Owner.Vars[$"hook_set_{handle.Name}"]
                .Call(new BishArgs([this, value])),
            _ => throw new ArgumentException("impossible!")
        };
    }

    public BishObject SetMember(string name, BishObject value, BishLookupMode mode = BishLookupMode.None) =>
        TrySetMember(name, value, mode) ?? throw BishException.OfAttribute("set", this, name);

    [Builtin("hook", tag: "ignore")]
    public static BishObject Set(BishObject self, BishString name, BishObject value) =>
        self.SetMember(name.Value, value);

    public BishObject DefMember(string name, BishObject value, BishLookupMode mode = BishLookupMode.None)
    {
        var self = Base(this, MRORoot);
        var hooked = this is BishType || mode.HasFlag(BishLookupMode.NoHook)
            ? null
            : self.TryCallHook("hook_def", new BishArgs([new BishString(name), value]), ignores: true);
        return hooked ??
               (mode.HasFlag(BishLookupMode.NoAccessor)
                   ? null
                   : self.TryCallHook($"hook_def_{name}", new BishArgs([value]))) ??
               (Vars[name] = value);
    }

    [Builtin("hook", tag: "ignore")]
    public static BishObject Def(BishObject self, BishString name, BishObject value) =>
        self.DefMember(name.Value, value);

    public BishObject DelMember(string name, BishLookupMode mode = BishLookupMode.None) =>
        TryDelMember(name, mode: mode) ?? throw BishException.OfAttribute("del", this, name);

    public BishObject? TryDelMember(string name, BishLookupMode mode = BishLookupMode.None)
    {
        var handle = TryGetHandle(name, "del", mode);
        if (handle is null) return null;
        return handle.Type switch
        {
            BishVarHandleType.Normal => handle.Owner.Vars.Remove(handle.Name, out var value) ? value : null,
            BishVarHandleType.Hook => handle.Owner.Vars["hook_del"]
                .Call(new BishArgs([this, new BishString(handle.Name)])),
            BishVarHandleType.Accessor => handle.Owner.Vars[$"hook_del_{handle.Name}"].Call(new BishArgs([this])),
            _ => throw new ArgumentException("impossible!")
        };
    }

    [Builtin("hook", tag: "ignore")]
    public static BishObject Del(BishObject self, BishString name) => self.DelMember(name.Value);

    public BishObject Call(BishArgs args) => TryCall(args) ?? throw BishException.OfType_NotCallable(this);

    public virtual BishObject? TryCall(BishArgs args) => TryGetMember("op_call")?.TryCall(args);

    public override string ToString() => throw new NotSupportedException("Use `show` or `debug` instead");

    [Builtin]
    public static BishString Repr(BishObject self, BishReprContext _) => new($"[Object {self.Type.Name}]");

    [Builtin("op")]
    public static BishBool Eq(BishObject a, BishObject b) => BishBool.Of(a == b);

    public T As<T>(string expr) where T : BishObject =>
        this as T ?? throw BishException.OfType_Expect(expr, this, typeof(T).Name);

    public BishObject As(BishType type, string expr) =>
        TryConvert(type) ?? throw BishException.OfType_Expect(expr, this, type);

    public BishObject? TryConvert(BishType type) => Type.CanAssignTo(type) ? this : null;

    [Builtin("op")]
    public static BishBool Neq(BishObject a, BishObject b) =>
        BishBool.Invert(BishOperator.Call("op_eq", new BishArgs([a, b]))
            .As<BishBool>($"{BishString.CallDebug(a)} == {BishString.CallDebug(b)}"));

    [Builtin("op")]
    public static BishBool Lt(BishObject a, BishObject b) => BishBool.Of(BishOperator.Cmp(a, b) < 0);

    [Builtin("op")]
    public static BishBool Le(BishObject a, BishObject b) => BishBool.Of(BishOperator.Cmp(a, b) <= 0);

    [Builtin("op")]
    public static BishBool Gt(BishObject a, BishObject b) => BishBool.Of(BishOperator.Cmp(a, b) > 0);

    [Builtin("op")]
    public static BishBool Ge(BishObject a, BishObject b) => BishBool.Of(BishOperator.Cmp(a, b) >= 0);

    [Builtin]
    public static BishBaseObject Base(BishObject self, [DefaultNull] BishType? root) => new(self,
        root ?? self.Type.LookupChain.ElementAtOrDefault(1)?.As<BishType>("base type") ??
        throw BishException.OfType_NoBase(self));

    [Builtin("hook")]
    public static BishProxyMap Get_vars(BishObject self) => new(self.Vars);

    [Builtin("hook")]
    public static BishType Get_type(BishObject self) => self.Type;

    [Builtin("hook")]
    public static BishType Set_type(BishObject self, BishType type) => self.Type = type;
}

public record BishVarHandle(BishObject Owner, string Name, BishVarHandleType Type, bool Bind = false);

public enum BishVarHandleType
{
    Normal,
    Hook,
    Accessor
}