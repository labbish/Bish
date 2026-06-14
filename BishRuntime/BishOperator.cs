using System.Text.RegularExpressions;

namespace BishRuntime;

public class Pattern
{
    private readonly object _pattern;

    private Pattern(object pattern) => _pattern = pattern;

    public static implicit operator Pattern(string pattern) => new(pattern);

    public static implicit operator Pattern(Regex pattern) => new(pattern);

    public bool Match(string name)
    {
        return _pattern switch
        {
            string pattern => pattern == name,
            Regex pattern => pattern.IsMatch(name),
            _ => throw new ArgumentException("Invalid pattern")
        };
    }

    public string Name => _pattern switch
    {
        string pattern => pattern,
        _ => throw new ArgumentException("Pattern is not string")
    };
}

public record SpecialMethod(Pattern NamePattern, string[]? Args, string? Op = null, Func<BishObject>? Default = null)
{
    public int? Argc => Args?.Length;

    public string OpString() => $"operator {Op} ({(Args is null ? "self, ..." : string.Join(", ", Args))})";
}

public static partial class BishOperator
{
    internal static readonly List<SpecialMethod> SpecialMethods =
    [
        new("repr", ["self", "ctx"]),
        new("hook_get", ["self", "member"], "get()"),
        new("hook_set", ["self", "member", "value"], "set()"),
        new("hook_def", ["self", "member", "value"], "def()"),
        new("hook_del", ["self", "member"], "del()"),
        new("hook_bind", ["self", "object"], "bind"),
        new("hook_new", null, "new"),
        new("op_eq", ["left", "right"], "==", () => BishBool.False),
        new("op_neq", ["left", "right"], "!="),
        new("op_call", null, "()"),
        new("op_pos", ["self"], "+"),
        new("op_neg", ["self"], "-"),
        new("op_add", ["left", "right"], "+"),
        new("op_sub", ["left", "right"], "-"),
        new("op_mul", ["left", "right"], "*"),
        new("op_div", ["left", "right"], "/"),
        new("op_mod", ["left", "right"], "%"),
        new("op_pow", ["left", "right"], "^"),
        new("op_cmp", ["left", "right"], "<=>"),
        new("op_lt", ["left", "right"], "<"),
        new("op_le", ["left", "right"], "<="),
        new("op_gt", ["left", "right"], ">"),
        new("op_ge", ["left", "right"], ">="),
        new("op_invert", ["self"], "~"),
        new("bool", ["self"], "bool", () => BishBool.True),
        new("nullish", ["self"], "nullish", () => BishBool.False),
        new(GetterRegex(), ["self"], "get"),
        new(SetterRegex(), ["self", "value"], "set"),
        new(DefferRegex(), ["self", "value"], "def"),
        new(DellerRegex(), ["self"], "del"),
        new("op_getIndex", ["self", "index"], "get[]"),
        new("op_setIndex", ["self", "index", "value"], "set[]"),
        new("op_defIndex", ["self", "index", "value"], "def[]"),
        new("op_delIndex", ["self", "index"], "del[]"),
        new("iter", ["self"], "iter"),
        new("dispose", ["self"], "dispose", () => BishNull.Instance)
    ];

    public static BishObject Call(string name, BishArgs args)
    {
        List<BishError> errors = [];
        foreach (var arg in args.Args)
        {
            try
            {
                if (arg.Type.TryGetMember(name, BishLookupMode.NoBind)?.TryCall(args) is { } result) return result;
            }
            catch (BishException e)
            {
                errors.Add(e.Error);
            }
        }

        var special = GetSpecialMethod(name);
        return special?.Default?.Invoke() ?? throw BishException.OfArgument_Operator(name, args.Args).CausedBy(errors);
    }

    public static bool Eq(BishObject a, BishObject b) => Call("op_eq", new BishArgs([a, b]))
        .As<BishBool>($"{BishString.CallDebug(a)} == {BishString.CallDebug(b)}").Value;

    public static SpecialMethod? GetSpecialMethod(string name) =>
        SpecialMethods.FirstOrDefault(s => s.NamePattern.Match(name));

    public static SpecialMethod GetOperator(string op, int argc)
    {
        var founds = SpecialMethods.Where(special => special.Op == op).ToList();
        if (founds.Count == 0) throw new ArgumentException($"No such operator: {op}");
        return founds.FirstOrDefault(special => special.Argc is null || special.Argc == argc) ??
               throw new ArgumentException(
                   $"Operator {op} does not take {argc} arguments; " +
                   $"did you mean {string.Join(" or ", founds.Select(special => special.OpString()))}");
    }

    public static string GetOperatorName(string op, int argc) => GetOperator(op, argc).NamePattern.Name;

    [GeneratedRegex("hook_get_[A-Za-z_][A-Za-z0-9_]*")]
    private static partial Regex GetterRegex();

    [GeneratedRegex("hook_set_[A-Za-z_][A-Za-z0-9_]*")]
    private static partial Regex SetterRegex();

    [GeneratedRegex("hook_def_[A-Za-z_][A-Za-z0-9_]*")]
    private static partial Regex DefferRegex();

    [GeneratedRegex("hook_del_[A-Za-z_][A-Za-z0-9_]*")]
    private static partial Regex DellerRegex();
}