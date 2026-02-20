namespace BishRuntime;

public static class BishOperator
{
    public static BishObject? TryCall(string name, List<BishObject> args)
    {
        return args.Select(arg => BishException.Ignored(() => arg.Type.GetMember(name).TryCall(args)))
            .FirstOrDefault(result => result is not null);
    }

    public static BishObject Call(string name, List<BishObject> args) =>
        TryCall(name, args) ?? throw BishException.OfType_Operator(name, args);
}