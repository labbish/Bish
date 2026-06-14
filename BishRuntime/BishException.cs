using BishUtils;

namespace BishRuntime;

public class BishException(BishError error) : Exception
{
    public BishError Error => error;
    public override string Message => Error.ToString();

    public static T Wrapped<T>(BishType errorType, Func<T> func)
    {
        try
        {
            return func();
        }
        catch (BishException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw Create(errorType, e.Message);
        }
    }

    public static void Wrapped(BishType errorType, Action func)
    {
        try
        {
            func();
        }
        catch (BishException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw Create(errorType, e.Message);
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

    public static BishException Create(BishType errorType, string message) =>
        new((BishError)errorType.CreateInstance(new BishArgs([new BishString(message)])));

    public static BishException OfNull(string op, string name) =>
        Create(BishError.NullErrorType, $"Cannot {op} member {name} on null")
            .With("operation", new BishString(op)).With("name", new BishString(name));

    public static BishException OfAttribute(string op, BishObject obj, string name) =>
        Create(BishError.AttributeErrorType, $"No such member: trying to {op} {name} on {BishString.CallDebug(obj)}")
            .With("operation", new BishString(op)).With("object", obj).With("name", new BishString(name));

    public static BishException OfType(string message) => Create(BishError.TypeErrorType, message);

    public static BishException OfType_NoBase(BishObject obj) =>
        OfType($"Cannot call .base() because MRO chain of {BishString.CallDebug(obj)} is empty").With("object", obj);

    public static BishException OfType_NotCallable(BishObject obj) =>
        OfType($"Cannot call {BishString.CallDebug(obj)}").With("object", obj);

    public static BishException OfType_Argument(BishObject obj, BishType expect) =>
        OfType($"Expect argument to be {expect.Name}, found {BishString.CallDebug(obj)}")
            .With("object", obj).With("expect", expect);

    public static BishException OfType_Expect(string expr, BishObject result, BishType expect) =>
        OfType($"Expect {expr} to be {expect.Name}, found {BishString.CallDebug(result)}")
            .With("expression", new BishString(expr)).With("result", result).With("expect", expect);

    public static BishException OfType_Expect(string expr, BishObject result, string expect) =>
        OfType($"Expect {expr} to be {expect}, found {BishString.CallDebug(result)}")
            .With("expression", new BishString(expr)).With("result", result).With("expect", new BishString(expect));

    public static BishException OfType_ErrorResult() => OfType("Cannot manually create ErrorResult");

    public static BishException OfType_Yield() =>
        Create(BishError.TypeErrorType, "Yield expression out of generator function");

    public static BishException OfType_Await() =>
        Create(BishError.TypeErrorType, "Await expression out of async function");

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
        OfArgument($"Cannot bind {obj} to {BishString.CallDebug(method)} because it takes no argument")
            .With("method", method).With("object", obj);

    public static BishException OfArgument_Operator(string op, IList<BishObject> args) =>
        OfArgument($"Cannot apply operator {op} on type{(args.Count > 1 ? "s" : "")} " +
                   string.Join(", ", args.Select(arg => arg.Type.Name)))
            .With("operator", new BishString(op)).With("arguments", new BishList(args.ToList()));

    public static BishException OfArgument_IndexOutOfBound(int length, int index) =>
        OfArgument($"Index out of bound: accessing index {index} with length {length}")
            .With("index", BishInt.Of(index)).With("length", BishInt.Of(length));

    public static BishException OfArgument_KeyNotFound(BishObject key) =>
        OfArgument($"Key {BishString.CallDebug(key)} does not exist").With("key", key);

    public static BishException OfArgument_MRO(BishType type) =>
        OfArgument($"Cannot create Consistent MRO for {type.Name}").With("type", type);

    public static BishException OfArgument_Parse(BishString str, BishType type) =>
        OfArgument($"Cannot parse {str.Value} to {type.Name}").With("type", type).With("string", str);

    public static BishException OfArgument_ListSetCount(int expect, int got) =>
        OfArgument($"Setting {expect} indexes with {got} elements")
            .With("expect", BishInt.Of(expect)).With("got", BishInt.Of(got));

    public static BishException OfArgument_RangeZeroStep() => OfArgument("Range step cannot be 0");

    public static BishException OfArgument_RangeNull() =>
        OfArgument("Cannot iterate range with start=null or end=null");

    public static BishException OfImport(string file, string message) =>
        Create(BishError.ImportErrorType, $"Cannot import {file}: {message}")
            .With("file", new BishString(file)).With("message", new BishString(message));

    public static BishException OfImport_NoFile(string path) =>
        OfImport(path, $"File doesn't exist: {path}").With("path", new BishString(path));

    public static BishException OfImport_InvalidExt(string file, string ext) =>
        OfImport(file, $"Invalid file extension: {ext}").With("extension", new BishString(ext));

    public static BishException OfImport_Dll(string file, Type[] types) =>
        OfImport(file, $"found types {string.Join(", ", types)}, none of which implements IPlugin")
            .With("types", new BishList(types.Select(type => new BishString(type.Name)).ToList<BishObject>()));

    public static BishException OfZeroDivision() => Create(BishError.ZeroDivisionErrorType, "Divided by zero");

    public static BishException OfCompile(string message) => Create(BishError.CompilationErrorType, message);

    public static BishException OfCompile_Errors(IList<CompilationError> errors) =>
        OfCompile("Compile error(s) occured").CausedBy(errors.Select(e => e.ToError()).ToList());

    public static BishException OfCompile_NoService() => OfCompile("Compile service is invalid!");

    public static BishException OfCompile_NoFile(string path) =>
        OfCompile($"File doesn't exist: {path}").With("path", new BishString(path));

    public static BishException OfCompile_InvalidExt(string ext) =>
        OfCompile($"Invalid file extension: {ext}").With("extension", new BishString(ext));

    public static BishException OfBytecodeParser(string message) => Create(BishError.BytecodeParserErrorType, message);

    public static BishException OfBytecodeParser_Magic() => OfBytecodeParser("Bad bytecode magic number!");

    public static BishException OfBytecodeParser_Version(int version, int expect) =>
        OfBytecodeParser($"Bad bytecode version {version}; expected {expect}!")
            .With("version", BishInt.Of(version)).With("expect", BishInt.Of(expect));

    public static BishException OfBytecodeParser_Invalid(string type) =>
        OfBytecodeParser($"Invalid bytecode: {type}").With("type", new BishString(type));
}