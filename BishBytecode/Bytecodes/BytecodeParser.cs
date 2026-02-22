using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BishBytecode.Bytecodes;

public static partial class BytecodeParser
{
    public static string ToString(BishBytecode bytecode)
    {
        var type = bytecode.GetType();
        var name = ToCodeName(type.Name);
        var args = Args(type);
        return name + " " + string.Join(" ",
            args.Select(arg => ArgToString(type.GetProperty(arg.Name)!.GetValue(bytecode)!)));
    }

    public static BishBytecode FromString(string code)
    {
        var parts = code.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var name = ToClassName(parts[0]);
        var type = typeof(BytecodeParser).Assembly.GetType($"{typeof(BytecodeParser).Namespace}.{name}") ??
                   throw new ArgumentException($"Invalid bytecode type {name} for BytecodeParser");
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

    private static string ToCodeName(string className)
    {
        return string.IsNullOrEmpty(className) ? className : CodeNameRegex().Replace(className, "_$1").ToUpper();
    }

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