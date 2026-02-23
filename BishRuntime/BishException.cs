namespace BishRuntime;

public class BishException(BishError error) : Exception
{
    public BishError Error => error;
    public override string Message => Error.ToString();

    public static T? Ignored<T>(Func<T?> func)
    {
        try
        {
            return func();
        }
        catch (BishException)
        {
            return default;
        }
    }

    public static BishException Create(BishType errorType, string message, Dictionary<string, BishObject> data)
    {
        var error = (BishError)errorType.CreateInstance([new BishString(message)]);
        foreach (var (key, value) in data)
            error.SetMember(key, value);
        return new BishException(error);
    }

    public static BishException OfNull(string op, string name) => Create(BishError.NullErrorType,
        $"Cannot {op} member {name} on null", new Dictionary<string, BishObject>
        {
            ["operation"] = new BishString(op),
            ["name"] = new BishString(name)
        });

    public static BishException OfAttribute(string op, BishObject obj, string name) =>
        Create(BishError.AttributeErrorType, $"No such member: trying to {op} {name} on {obj}",
            new Dictionary<string, BishObject>
            {
                ["operation"] = new BishString(op),
                ["object"] = obj,
                ["name"] = new BishString(name)
            });

    public static BishException OfType(string message, Dictionary<string, BishObject> data) =>
        Create(BishError.TypeErrorType, message, data);

    public static BishException OfType_NotCallable(BishObject obj) => OfType($"Cannot call {obj}",
        new Dictionary<string, BishObject>
        {
            ["object"] = obj
        });

    public static BishException OfType_Argument(BishObject obj, BishType expect) =>
        OfType($"Expect argument to be {expect}, found {obj}", new Dictionary<string, BishObject>
        {
            ["object"] = obj,
            ["expect"] = expect
        });

    public static BishException OfType_Expect(string expr,
        BishObject result, BishType expect)
        => OfType($"Expect result of {expr} to be ${expect.Name}, found {result}",
            new Dictionary<string, BishObject>
            {
                ["expression"] = new BishString(expr),
                ["result"] = result,
                ["expect"] = expect
            });

    public static BishException OfArgument(string message, Dictionary<string, BishObject> data) =>
        Create(BishError.ArgumentErrorType, message, data);

    public static BishException OfArgument_DefineRepeat(string name) => OfArgument(
        $"Found repeated argument {name} while defining function", new Dictionary<string, BishObject>
        {
            ["name"] = new BishString(name)
        });

    public static BishException OfArgument_DefineDefault(int index, string name) => OfArgument(
        $"Found required argument {name} at index {index}, after some optional argument, while defining function",
        new Dictionary<string, BishObject>
        {
            ["index"] = new BishInt(index),
            ["name"] = new BishString(name)
        });

    public static BishException OfArgument_Count(int min, int max, int count) => OfArgument(
        $"Wrong argument count: expected {(min == max ? min : $"{min}~{max}")}, got {count}",
        new Dictionary<string, BishObject>
        {
            ["min"] = new BishInt(min),
            ["max"] = new BishInt(max),
            ["count"] = new BishInt(count)
        });

    public static BishException OfArgument_Bind(BishFunc method, BishObject obj) => OfArgument(
        $"Cannot bind {obj} to {method} because {method} takes no argument",
        new Dictionary<string, BishObject>
        {
            ["method"] = method,
            ["object"] = obj
        });

    public static BishException OfArgument_Operator(string op, List<BishObject> args) => OfArgument(
        $"Cannot apply operator {op} on type(s) {string.Join(", ", args.Select(arg => arg.Type.Name))}",
        new Dictionary<string, BishObject>
        {
            ["operator"] = new BishString(op) // TODO: record the arg types after we have an builtin list
        });

    public static BishException OfArgument_IndexOutOfBound(BishObject obj, int index) => OfArgument(
        $"Index out of bound: accessing index {index} on {obj}",
        new Dictionary<string, BishObject>
        {
            ["index"] = new BishInt(index),
            ["object"] = obj
        }
    );

    public static BishException OfName(string name) => Create(BishError.NameErrorType,
        $"Name {name} is not defined", new Dictionary<string, BishObject>
        {
            ["name"] = new BishString(name)
        });

    public static BishException OfZeroDivision() => Create(BishError.ZeroDivisionErrorType, "Divided by zero", []);

    public static BishException OfIteratorStop() => Create(BishError.IteratorStopType, "Iterator stopped", []);
}