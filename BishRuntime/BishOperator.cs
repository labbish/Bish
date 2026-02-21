namespace BishRuntime;

public record SpecialMethod(string Name, int? Argc, string? Op = null);

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
        new("op_Eq", 2, "=="),
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
        new("op_Ge", 2, ">=")
    ];

    public static BishObject? TryCall(string name, List<BishObject> args)
    {
        return args.Select(arg =>
                BishException.Ignored(() => arg.Type.GetMember(name, BishLookupMode.NoBind).TryCall(args)))
            .FirstOrDefault(result => result is not null);
    }

    public static BishObject Call(string name, List<BishObject> args) =>
        TryCall(name, args) ?? throw BishException.OfArgument_Operator(name, args);

    internal static void CheckSpecialMethod(string name, BishFunc func)
    {
        const string tip = "; consider add `special: false` if this is intended.";
        var special = SpecialMethods.FirstOrDefault(s => s.Name == name);
        if (special is null) throw new ArgumentException($"'{name}' is not a special name" + tip);
        if (special.Argc is not null && special.Argc != func.Args.Count)
            throw new ArgumentException($"${special.Name} expects {special.Argc} args, found {func.Args.Count}" + tip);
    }
}