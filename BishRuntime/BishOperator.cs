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

public record SpecialMethod(Pattern NamePattern, int? Argc, string? Op = null, Func<BishObject>? Default = null);

public static partial class BishOperator
{
    internal static readonly List<SpecialMethod> SpecialMethods =
    [
        new("toString", 1),
        new("hook_Get", 2),
        new("hook_Set", 3),
        new("hook_Del", 2),
        new("hook_Create", 0),
        new("hook_Init", null),
        new("op_Eq", 2, "==", () => new BishBool(false)),
        new("op_Neq", 2, "!="),
        new("op_Call", null, "()"),
        new("op_Pos", 1, "+"),
        new("op_Neg", 1, "-"),
        new("op_Add", 2, "+"),
        new("op_Sub", 2, "-"),
        new("op_Mul", 2, "*"),
        new("op_Div", 2, "/"),
        new("op_Mod", 2, "%"),
        new("op_Pow", 2, "^"),
        new("op_Cmp", 2, "<=>"),
        new("op_Lt", 2, "<"),
        new("op_Le", 2, "<="),
        new("op_Gt", 2, ">"),
        new("op_Ge", 2, ">="),
        new("op_Invert", 1, "~"),
        new("op_Bool", 1, "bool", () => new BishBool(true)),
        new(GetterRegex(), 1),
        new(SetterRegex(), 2),
        new(DellerRegex(), 1),
        new("op_GetIndex", 2, "get[]"),
        new("op_SetIndex", 3, "set[]"),
        new("op_DelIndex", 2, "del[]"),
        new("op_Iter", 1, "iter")
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
        if (special.Argc is not null && special.Argc != func.Args.Count)
            throw new ArgumentException($"{opName} expects {special.Argc} args, found {func.Args.Count}" + tip);
    }

    public static string GetOperatorName(string op, int argc) =>
        SpecialMethods.First(special => special.Op == op && special.Argc == argc).NamePattern.Name;

    [GeneratedRegex("hook_Get_[A-Za-z_]+")]
    private static partial Regex GetterRegex();

    [GeneratedRegex("hook_Set_[A-Za-z_]+")]
    private static partial Regex SetterRegex();

    [GeneratedRegex("hook_Del_[A-Za-z_]+")]
    private static partial Regex DellerRegex();
}