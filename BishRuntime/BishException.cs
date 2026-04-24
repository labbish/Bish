using BishUtils;

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

    public BishException CausedBy(params IList<BishError> causes)
    {
        Error.Causes = causes.ToConcurrentList();
        return this;
    }

    public BishException With(string key, BishObject value)
    {
        Error.DefMember(key, value);
        return this;
    }

    public static BishException Create(BishType errorType, string message)
    {
        var error = (BishError)errorType.CreateInstance([new BishString(message)]);
        return new BishException(error);
    }

    public static BishException OfNull(string op, string name) =>
        Create(BishError.NullErrorType, $"Cannot {op} member {name} on null")
            .With("operation", new BishString(op)).With("name", new BishString(name));

    public static BishException OfAttribute(string op, BishObject obj, string name) =>
        Create(BishError.AttributeErrorType, $"No such member: trying to {op} {name} on {obj}")
            .With("operation", new BishString(op)).With("object", obj).With("name", new BishString(name));

    public static BishException OfType(string message) => Create(BishError.TypeErrorType, message);

    public static BishException OfType_NoBase(BishObject obj) =>
        OfType($"Cannot call .base() because MRO chain of {obj} is empty").With("object", obj);

    public static BishException OfType_NotCallable(BishObject obj) =>
        OfType($"Cannot call {obj}").With("object", obj);

    public static BishException OfType_Argument(BishObject obj, BishType expect) =>
        OfType($"Expect argument to be {expect}, found {obj}")
            .With("object", obj).With("expect", expect);

    public static BishException OfType_Expect(string expr, BishObject result, BishType expect) =>
        OfType($"Expect result of {expr} to be {expect.Name}, found {result}")
            .With("expression", new BishString(expr)).With("result", result).With("expect", expect);

    public static BishException OfType_Expect(string expr, BishObject result, string expect) =>
        OfType($"Expect {expr} to be {expect}, found {result}").With("expression", new BishString(expr))
            .With("result", result).With("expect", new BishString(expect));

    public static BishException OfType_ErrorResult() => OfType("Cannot manually create ErrorResult");

    public static BishException OfArgument(string message) => Create(BishError.ArgumentErrorType, message);

    public static BishException OfArgument_DefineRepeat(string name) => OfArgument(
        $"Found repeated argument {name} while defining function").With("name", new BishString(name));

    public static BishException OfArgument_DefineDefault(int index, string name) =>
        OfArgument(
                $"Found required argument {name} at index {index}, after optional arguments, while defining function")
            .With("index", BishInt.Of(index)).With("name", new BishString(name));

    public static BishException OfArgument_DefineRests() =>
        OfArgument("Found multiple rest args while defining function");

    public static BishException OfArgument_DefineRestPos() =>
        OfArgument("Found rest argument which is not the last argument while defining function");

    public static BishException OfArgument_DefineRestDefault() =>
        OfArgument("Function definition contains both rest argument and optional argument");

    private static string Range(int? min, int? max)
    {
        if (min is null) return $"<={max}";
        if (max is null) return $">={min}";
        return min == max ? $"{min}" : $"{min}~{max}";
    }

    public static BishException OfArgument_Count(int count, int? min = null, int? max = null) =>
        OfArgument($"Wrong argument count: expected {Range(min, max)}, got {count}")
            .With("min", min is null ? BishNull.Instance : BishInt.Of(min.Value))
            .With("max", max is null ? BishNull.Instance : BishInt.Of(max.Value))
            .With("count", BishInt.Of(count));

    public static BishException OfArgument_Bind(BishFunc method, BishObject obj) =>
        OfArgument($"Cannot bind {obj} to {method} because {method} takes no argument")
            .With("method", method).With("object", obj);

    public static BishException OfArgument_Operator(string op, IList<BishObject> args) =>
        OfArgument($"Cannot apply operator {op} on type{(args.Count > 1 ? "s" : "")} " +
                   string.Join(", ", args.Select(arg => arg.Type.Name)))
            .With("operator", new BishString(op)).With("arguments", new BishList(args.ToList()));

    public static BishException OfArgument_IndexOutOfBound(int length, int index) =>
        OfArgument($"Index out of bound: accessing index {index} with length {length}")
            .With("index", BishInt.Of(index)).With("length", BishInt.Of(length));

    public static BishException OfArgument_KeyNotFound(BishObject key) =>
        OfArgument($"Key {key} does not exist").With("key", key);

    public static BishException OfArgument_MRO(BishType type) =>
        OfArgument($"Cannot create Consistent MRO for {type}").With("type", type);

    public static BishException OfArgument_Parse(BishString str, BishType type) =>
        OfArgument($"Cannot parse {str} to {type}").With("type", type).With("string", str);

    public static BishException OfArgument_ListSetCount(int expect, int got) =>
        OfArgument($"Setting {expect} indexes with {got} elements")
            .With("expect", BishInt.Of(expect)).With("got", BishInt.Of(got));

    public static BishException OfArgument_RangeZeroStep() => OfArgument("Range step cannot be 0");

    public static BishException OfArgument_RangeNull() =>
        OfArgument("Cannot iterate range with start=null or end=null");

    public static BishException OfImport(string file, string message) =>
        Create(BishError.ImportErrorType, $"Cannot import {file}: {message}")
            .With("file", new BishString(file)).With("message", new BishString(message));

    public static BishException OfZeroDivision() => Create(BishError.ZeroDivisionErrorType, "Divided by zero");

    public static BishException OfIteratorStop() => Create(BishError.IteratorStopType, "Iterator stopped");

    public static BishException OfYield(BishObject value) =>
        Create(BishError.YieldValueType, "Yield expression out of generator function").With("value", value);
}