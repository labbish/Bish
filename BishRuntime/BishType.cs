using BishUtils;

namespace BishRuntime;

public partial class BishType(string name, IEnumerable<BishType>? parents = null, int skips = 0) : BishObject
{
    public string Name { get; private set; } = name;
    public IList<BishType> Parents { get; private set; } = (parents ?? []).ToConcurrentList();
    public readonly int Skips = skips;

    protected override IList<BishObject> LookupChain =>
        GetMRO().Concat([BishObject.StaticType]).Skip(Skips).ToConcurrentList<BishObject>();

    [Builtin("hook")]
    public static BishType Create(BishObject _) => new(null!);

    [Builtin("hook")]
    public static void Init(BishType self, BishString name, [DefaultNull] BishList? parents)
    {
        self.Name = name.Value;
        if (parents is null) return;
        self.Parents = parents.List.Select(parent => parent.ExpectToBe<BishType>("parent type")).ToList();
        self.ClearMROCache();
    }

    public BishObject CreateInstance(IList<BishObject> args)
    {
        var instance = new BishObject();
        var types = GetMRO();
        types.Reverse();
        foreach (var type in types)
        {
            var created = type.Vars.GetValueOrDefault("hook_create")?.TryCall([instance]);
            instance = created ?? instance;
            instance.Type = type;
        }

        instance.TryCallHook("hook_init", args);
        return instance;
    }

    public override BishObject TryCall(IList<BishObject> args) => CreateInstance(args);

    public bool CanAssignTo(BishType other) => this == other || LookupChain.Contains(other);

    public override string ToString() => Name;

    [Builtin("hook")]
    public static BishString Get_name(BishType type) => new(type.Name);

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("type");

    internal static BishType GetStaticType(Type type) =>
        type.GetField("StaticType")?.GetValue(null) as BishType ??
        throw new ArgumentException($"Cannot find field `StaticType` on type {type}");

    public BishType WithMRORoot(BishType mroRoot) => mroRoot == this
        ? this
        : new BishType(Name, Parents,
            GetMRO().Concat([BishObject.StaticType]).ToList().FindIndex(type => type == mroRoot)) { Vars = Vars };

    [Builtin("hook")]
    public static BishList Get_parents(BishType self) => new(new ParentsProxyList(self));

    [Builtin("hook")]
    public static BishList Get_MRO(BishType self) => new(self.GetMRO().ToList<BishObject>());
}

public class ParentsProxyList(BishType type) : ProxyList<BishType>(type.Parents)
{
    public BishType Type => type;

    protected override BishObject ToItem(BishType source) => source;

    protected override BishType ToSource(BishObject item) => item.ExpectToBe<BishType>("type");

    protected override void OnModify() => Type.ClearMROCache();
}