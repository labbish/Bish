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

    public string ExpectArgc() => Argc?.ToString() ?? ">= 1";
}

public static partial class BishOperator
{
    internal static readonly List<SpecialMethod> SpecialMethods =
    [
        new("toString", ["self"]),
        new("hook_get", ["self", "member"], "get()"),
        new("hook_set", ["self", "member", "value"], "set()"),
        new("hook_del", ["self", "member"], "del()"),
        new("hook_create", ["self"], "create"),
        new("hook_init", null, "init"),
        new("op_eq", ["left", "right"], "==", () => new BishBool(false)),
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
        new("op_bool", ["self"], "bool", () => new BishBool(true)),
        new(GetterRegex(), ["self"], "get"),
        new(SetterRegex(), ["self", "value"], "set"),
        new(DellerRegex(), ["self"], "del"),
        new("op_getIndex", ["self", "index"], "get[]"),
        new("op_setIndex", ["self", "index", "value"], "set[]"),
        new("op_delIndex", ["self", "index"], "del[]"),
        new("iter", ["self"], "iter")
    ];

    public static BishObject? TryCall(string name, List<BishObject> args) =>
        BishException.Ignored(() => Call(name, args));

    public static BishObject Call(string name, List<BishObject> args, bool noSpecial = false)
    {
        var special = GetSpecialMethod(name);
        if (!noSpecial && special?.Default is not null)
            return BishException.Ignored(() => Call(name, args, true)) ?? special.Default();

        List<BishError> errors = [];
        foreach (var arg in args)
        {
            try
            {
                return arg.Type.GetMember(name, BishLookupMode.NoBind).Call(args);
            }
            catch (BishException e)
            {
                errors.Add(e.Error);
            }
        }

        throw BishException.OfArgument_Operator(name, args).CausedBy(errors);
    }

    public static SpecialMethod? GetSpecialMethod(string name)
    {
        return SpecialMethods.FirstOrDefault(s => s.NamePattern.Match(name));
    }

    internal static void CheckSpecialMethod(string name, BishFunc func)
    {
        const string tip = "; consider add `special: false` if this is intended.";
        var special = GetSpecialMethod(name);
        if (special is null) throw new ArgumentException($"'{name}' is not a special name" + tip);
        var opName = special.Op is null ? name : $"{name} (operator {special.Op})";
        var argc = func.Args.Count;
        if ((special.Argc is null && argc != 0) || special.Argc == argc) return;
        throw new ArgumentException($"{opName} expects {special.ExpectArgc()} args, found {func.Args.Count}" + tip);
    }

    public static SpecialMethod GetOperator(string op, int argc)
    {
        var founds = SpecialMethods.Where(special => special.Op == op).ToList();
        return founds.FirstOrDefault(special => (special.Argc is null && argc > 0) || special.Argc == argc) ??
               throw new ArgumentException(
                   $"Operator {op} does not take {argc} arguments; " +
                   $"did you mean {string.Join(" or ", founds.Select(special => special.OpString()))}");
    }

    public static string GetOperatorName(string op, int argc) => GetOperator(op, argc).NamePattern.Name;

    [GeneratedRegex("hook_get_[A-Za-z_][A-Za-z0-9_]*")]
    private static partial Regex GetterRegex();

    [GeneratedRegex("hook_set_[A-Za-z_][A-Za-z0-9_]*")]
    private static partial Regex SetterRegex();

    [GeneratedRegex("hook_del_[A-Za-z_][A-Za-z0-9_]*")]
    private static partial Regex DellerRegex();
}