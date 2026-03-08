using System.Collections;

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
            GetMRO().Concat([BishObject.StaticType]).ToList().FindIndex(type => type == mroRoot)) { Members = Members };

    public override BishTypeReflect Reflect() => new(this);

    static BishType() => BishBuiltinBinder.Bind<BishType>();
}

public class BishTypeReflect(BishType type) : BishReflect(type)
{
    public new BishType Type => type;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("reflect", [BishReflect.StaticType]);

    [Builtin("hook")]
    public static BishList Get_parents(BishTypeReflect self) => new(new ParentsProxyList(self.Type, self.Type.Parents));

    [Builtin("hook")]
    public static BishList Get_MRO(BishTypeReflect self) => new(self.Type.GetMRO().ToList<BishObject>());

    static BishTypeReflect() => BishBuiltinBinder.Bind<BishTypeReflect>();
}

public class ParentsProxyList(BishType type, List<BishType> list) : IList<BishObject>
{
    public BishType Type => type;
    public List<BishType> List => list;

    public IEnumerator<BishObject> GetEnumerator() => list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private BishType Convert(BishObject item)
    {
        if (item is not BishType t) throw BishException.OfType_Argument(item, BishType.StaticType);
        Refresh();
        return t;
    }

    private void Refresh() => Type.ClearMROCache();

    public void Add(BishObject item) => List.Add(Convert(item));

    public void Clear()
    {
        Refresh();
        List.Clear();
    }

    public bool Contains(BishObject item) => item is BishType t && List.Contains(t);

    public void CopyTo(BishObject[] array, int arrayIndex)
    {
        // God knows why I can't use List.CopyTo(array, arrayIndex). It looks pretty safe to me.
        for (var i = 0; i < List.Count; i++) array[arrayIndex + i] = List[i];
    }

    public bool Remove(BishObject item)
    {
        Refresh();
        return item is BishType t && List.Remove(t);
    }

    public int Count => List.Count;
    public bool IsReadOnly => false;
    public int IndexOf(BishObject item) => item is BishType t ? List.IndexOf(t) : -1;

    public void Insert(int index, BishObject item) => List.Insert(index, Convert(item));

    public void RemoveAt(int index)
    {
        Refresh();
        List.RemoveAt(index);
    }

    public BishObject this[int index]
    {
        get => List[index];
        set => List[index] = Convert(value);
    }
}