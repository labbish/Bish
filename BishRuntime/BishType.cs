using BishUtils;

namespace BishRuntime;

public partial class BishType(string name, IEnumerable<BishType>? parents = null, int skips = 0) : BishObject
{
    public readonly string Name = name;
    protected readonly IList<BishType> Parents = (parents ?? []).ToConcurrentList();
    public readonly int Skips = skips;

    public ParentsProxyList ParentsProxy => new(this, Parents);

    protected override IList<BishObject> LookupChain =>
        GetMRO().Concat([BishObject.StaticType]).Skip(Skips).ToConcurrentList<BishObject>();

    [Builtin("hook")]
    public static BishType New(BishString name, [DefaultNull] BishList? parents) =>
        new(name.Value, parents?.List.Select(parent => parent.As<BishType>("parent type")).ToList());

    public BishObject CreateInstance(IList<BishObject> args)
    {
        var hook = TryGetMember("hook_new",
            BishLookupMode.NoAccessor | BishLookupMode.NoHook | BishLookupMode.NotFromType);
        var instance = hook?.Call(args) ?? new BishObject();
        instance.Type = this;
        return instance;
    }

    public override BishObject TryCall(IList<BishObject> args) => CreateInstance(args);

    public bool CanAssignTo(BishType other) => this == other || LookupChain.Contains(other);

    [Builtin]
    public static BishString Repr(BishType self, BishReprContext _) => new(self.Name);

    [Builtin("hook")]
    public static BishString Get_name(BishType type) => new(type.Name);

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("type");

    public BishType WithMRORoot(BishType mroRoot) => mroRoot == this
        ? this
        : new BishType(Name, Parents,
            GetMRO().Concat([BishObject.StaticType]).ToList().FindIndex(type => type == mroRoot)) { Vars = Vars };

    [Builtin("hook")]
    public static BishList Get_parents(BishType self) => new(self.ParentsProxy);

    [Builtin("hook")]
    public static BishType? Get_parent(BishType self) => self.GetMRO().Skip(1).FirstOrDefault();

    [Builtin("hook")]
    public static BishList Get_MRO(BishType self) => new(self.GetMRO().ToList<BishObject>());
}

public class ParentsProxyList(BishType type, IList<BishType> parents) : ProxyList<BishType>(parents)
{
    public BishType Type => type;

    protected override BishObject ToItem(BishType source) => source;

    protected override BishType ToSource(BishObject item) => item.As<BishType>("type");

    protected override void OnModify() => Type.ClearMROCache();
}