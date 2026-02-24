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

    public BishException CausedBy(params List<BishError> causes)
    {
        Error.Causes = causes;
        return this;
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
        => OfType($"Expect result of {expr} to be {expect.Name}, found {result}",
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

    public static BishException OfArgument_DefineRests() =>
        OfArgument("Found multiple rest args while defining function", []);

    public static BishException OfArgument_DefineRestPos() =>
        OfArgument("Found rest argument which is not the last argument while defining function", []);

    public static BishException OfArgument_DefineRestDefault() =>
        OfArgument("Function definition contains both rest argument and optional argument", []);

    private static string Range(int? min, int? max)
    {
        if (min is null) return $"<={max}";
        if (max is null) return $">={min}";
        return min == max ? $"{min}" : $"{min}~{max}";
    }

    public static BishException OfArgument_Count(int count, int? min = null, int? max = null) => OfArgument(
        $"Wrong argument count: expected {Range(min, max)}, got {count}",
        new Dictionary<string, BishObject>
        {
            ["min"] = min is null ? BishNull.Instance : new BishInt(min.Value),
            ["max"] = max is null ? BishNull.Instance : new BishInt(max.Value),
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
        $"Cannot apply operator {op} on type{(args.Count > 1 ? "s" : "")} " +
        string.Join(", ", args.Select(arg => arg.Type.Name)),
        new Dictionary<string, BishObject>
        {
            ["operator"] = new BishString(op),
            ["arguments"] = new BishList(args.ToList())
        });

    public static BishException OfArgument_IndexOutOfBound(int length, int index) => OfArgument(
        $"Index out of bound: accessing index {index} with length {length}",
        new Dictionary<string, BishObject>
        {
            ["index"] = new BishInt(index),
            ["length"] = new BishInt(length)
        }
    );

    public static BishException OfArgument_MRO(BishType type) => OfArgument(
        $"Cannot create Consistent MRO for {type}",
        new Dictionary<string, BishObject>
        {
            ["type"] = type
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