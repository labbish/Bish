namespace BishRuntime;

public record SpecialMethod(string Name, int? Argc, string? Op = null, Func<BishObject>? Default = null);

public static class BishOperator
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
        new("op_Bool", 1, "bool", () => new BishBool(true))
    ];

    public static BishObject? TryCall(string name, List<BishObject> args, bool noSpecial = false)
    {
        var special = GetSpecialMethod(name);
        if (!noSpecial && special?.Default is not null)
            return BishException.Ignored(() => TryCall(name, args, true)) ?? special.Default();
        return args.Select(arg =>
                BishException.Ignored(() => arg.Type.GetMember(name, BishLookupMode.NoBind).TryCall(args)))
            .FirstOrDefault(result => result is not null);
    }

    // TODO: wrap the inner error(s)
    public static BishObject Call(string name, List<BishObject> args) =>
        TryCall(name, args) ?? throw BishException.OfArgument_Operator(name, args);

    public static SpecialMethod? GetSpecialMethod(string name) => SpecialMethods.FirstOrDefault(s => s.Name == name);

    internal static void CheckSpecialMethod(string name, BishFunc func)
    {
        const string tip = "; consider add `special: false` if this is intended.";
        var special = GetSpecialMethod(name);
        if (special is null) throw new ArgumentException($"'{name}' is not a special name" + tip);
        var opName = special.Op is null ? name : $"{name} (operator {special.Op})";
        if (special.Argc is not null && special.Argc != func.Args.Count)
            throw new ArgumentException($"{opName} expects {special.Argc} args, found {func.Args.Count}" + tip);
    }
}