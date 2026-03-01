namespace BishRuntime;

public partial class BishType(string name, List<BishType>? parents = null, int skips = 0) : BishObject
{
    public readonly string Name = name;
    public readonly List<BishType> Parents = parents ?? [];
    public readonly int Skips = skips;

    protected override List<BishObject> LookupChain =>
        GetMRO().Concat([BishObject.StaticType]).Skip(Skips).ToList<BishObject>();

    public BishObject CreateInstance(List<BishObject> args)
    {
        var instance = new BishObject();
        var types = GetMRO();
        types.Reverse();
        foreach (var type in types)
        {
            var created = type.Members.GetValueOrDefault("hook_create")?.TryCall([instance]);
            instance = created ?? instance;
            instance.Type = type;
        }

        instance.TryCallHook("hook_init", args);
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

    public override BishTypeReflect Reflect() => new(this);

    static BishType() => BishBuiltinBinder.Bind<BishType>();
}

public class BishTypeReflect(BishType type) : BishReflect(type)
{
    public new BishType Type => type;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("reflect", [BishReflect.StaticType]);

    [Builtin("hook")]
    public static BishList Get_parents(BishTypeReflect self) =>
        new(new BishTypedListProxy<BishType>(self.Type.Parents));
    
    [Builtin("hook")]
    public static BishList Get_MRO(BishTypeReflect self) => new(self.Type.GetMRO().ToList<BishObject>());

    static BishTypeReflect() => BishBuiltinBinder.Bind<BishTypeReflect>();
}