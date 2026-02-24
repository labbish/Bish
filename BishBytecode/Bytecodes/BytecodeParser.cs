using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BishBytecode.Bytecodes;

public static partial class BytecodeParser
{
    // Reserved for user-defined bytecodes in other assemblies
    // ReSharper disable once CollectionNeverUpdated.Global
    public static readonly Dictionary<string, Type> Mappings = [];

    public static string ToString(BishBytecode bytecode)
    {
        var type = bytecode.GetType();
        var name = Mappings.FirstOrDefault(pair => pair.Value == type).Key ?? ToCodeName(type.Name);
        var args = Args(type);
        return (bytecode.Tag is null ? "" : bytecode.Tag + ": ") + name + " " + string.Join(" ",
            args.Select(arg => ArgToString(type.GetProperty(arg.Name)!.GetValue(bytecode)!)));
    }

    public static BishBytecode FromString(string code) =>
        FromString(code.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public static BishBytecode FromString(string[] parts)
    {
        if (parts[0].EndsWith(':')) return FromString(parts[1..]).Tagged(parts[0][..^1]);
        var first = parts[0].ToUpper();
        var type =
            Mappings.GetValueOrDefault(first) ??
            typeof(BytecodeParser).Assembly.GetType($"{typeof(BytecodeParser).Namespace}.{ToClassName(first)}") ??
            throw new ArgumentException($"Invalid bytecode type {first} for BytecodeParser");
        var args = Args(type);
        return (BishBytecode)type.GetConstructors().First()
            .Invoke(args.Select((arg, i) => ArgFromString(arg.Type, parts[i + 1])).ToArray());
    }

    private static string ArgToString(object value)
    {
        return value switch
        {
            int x => x.ToString(),
            double x => x.ToString(CultureInfo.InvariantCulture),
            string x => x,
            IList list => "[" + string.Join(",", list.OfType<object>().Select(ArgToString)) + "]",
            _ => throw new ArgumentException($"Invalid argument type {value.GetType()} for BytecodeParser")
        };
    }

    private static object ArgFromString(Type type, string str)
    {
        if (type == typeof(int)) return int.Parse(str);
        if (type == typeof(double)) return double.Parse(str);
        if (type == typeof(string)) return str;
        if (typeof(IList).IsAssignableFrom(type))
        {
            var innerType = type.GetGenericArguments()[0];
            var collection = str.TrimStart("[").TrimEnd("]").ToString().Split(",")
                .Select(part => ArgFromString(innerType, part));
            // collection.Cast<T>().ToList()
            var castMethod = typeof(Enumerable).GetMethod("Cast")!.MakeGenericMethod(innerType);
            var toListMethod = typeof(Enumerable).GetMethod("ToList")!.MakeGenericMethod(innerType);
            return toListMethod.Invoke(null, [castMethod.Invoke(null, [collection])])!;
        }

        throw new ArgumentException($"Invalid argument type {type} for BytecodeParser");
    }

    private static List<(Type Type, string Name)> Args(Type type)
    {
        return type.GetConstructors().First().GetParameters().Select(p => (p.ParameterType, p.Name!)).ToList();
    }

    private static string ToCodeName(string className) => string.IsNullOrEmpty(className)
        ? className
        : CodeNameRegex().Replace(className, "_$1").ToUpper();

    private static string ToClassName(string codeName)
    {
        return string.IsNullOrEmpty(codeName)
            ? codeName
            : string.Join("", codeName.Split('_')
                .Select(word => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(word.ToLower())));
    }

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex CodeNameRegex();
}