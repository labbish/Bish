namespace BishRuntime;

public partial class BishType
{
    public List<BishType> GetMRO()
    {
        if (Parents.Count == 0) return [this];
        var parentMRO = Parents.Select(p => p.GetMRO()).ToList();
        List<List<BishType>> sequences = [..parentMRO, [..Parents]];
        return [this, ..Merge(sequences)];
    }

    private List<BishType> Merge(List<List<BishType>> sequences)
    {
        List<BishType> result = [];
        var current = sequences.Select(s => s.ToList()).ToList();
        while (true)
        {
            current.RemoveAll(s => s.Count == 0);
            if (current.Count == 0) break;
            var candidate = current
                .Select(seq => seq[0])
                .FirstOrDefault(head => !current.Any(s => s.Skip(1).Contains(head)));
            if (candidate is null) throw BishException.OfArgument_MRO(this);
            result.Add(candidate);
            foreach (var seq in current.Where(seq => seq.Count > 0 && seq[0] == candidate))
                seq.RemoveAt(0);
        }

        return result;
    }
}

public class BishBaseObject(BishObject inner, BishType mroRoot) : BishObject
{
    public BishObject Inner => inner;
    public BishType MRORoot => mroRoot;

    public override BishType DefaultType => inner.Type.WithMRORoot(MRORoot);

    public override BishObject? TryGetMember(string name, BishLookupMode mode = BishLookupMode.None,
        BishType? _ = null, List<BishObject>? excludes = null, BishObject? boundSelf = null) =>
        Inner.TryGetMember(name, mode, MRORoot, excludes, boundSelf ?? this);

    public override BishObject SetMember(string name, BishObject value, BishObject? root = null) =>
        Inner.SetMember(name, value, root ?? MRORoot);

    public override BishObject? TryDelMember(string name, BishObject? root = null) => Inner.TryDelMember(name, root);

    public override string ToString() => $"base({inner}, root={MRORoot})";
}