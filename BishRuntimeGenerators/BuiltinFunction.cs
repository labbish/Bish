namespace BishRuntimeGenerators;

public record BuiltinFunctionArg(string Type, string Name, bool IsDefault, bool IsRest)
{
    public override string ToString() =>
        $"""new BishArg("{Name}", {(IsDefault ? "new DefaultNull()" : "null")}, {IsRest.ToString().ToLower()})""";
}

public record BuiltinFunction(
    string Type,
    string? Prefix,
    string Name,
    BuiltinFunctionArg[] Args,
    string? Tag,
    bool IsVoid,
    bool IsNullable,
    bool PassCaller,
    string? Namespace) : IBuiltinStuff
{
    public string FullName => (Prefix is null ? "" : $"{Prefix}_") + Name.Lower();

    // We'll use a simple fix for now
    public static string Full(string type) => type == "Stop" ? "BishIterator.Stop" : type;

    public string Code =>
        $$"""
          {{Full(Type)}}.StaticType.DefMember("{{FullName}}", new BishNativeFunc("{{FullName}}", 
              [{{string.Join(", ", Args.Select(a => a.ToString()))}}],
              raw_arg_list => {
                  {{string.Join(" ", Args.Select((arg, i) => $"var raw_{arg.Name} = raw_arg_list[{i}];"))}}
                  {{string.Join(" ", Args.Select(arg =>
                      $"var {arg.Name} = {(arg.IsDefault ? $"raw_{arg.Name} is DefaultNull ? null : " : "")}" +
                      $"({Full(arg.Type)})raw_{arg.Name}.As({Full(arg.Type)}.StaticType, \"{arg.Name}\");"))}}
                  {{(IsVoid ? "" : "return ")}}{{Full(Type)}}.{{Name}}({{string.Join(", ", Args.Select(arg => arg.Name))}}){{(IsNullable ? " ?? (BishObject)BishNull.Instance" : "")}};
                  {{(IsVoid ? "return BishNull.Instance;" : "")}}
              },
              "{{Tag ?? ""}}",
              passCaller: {{PassCaller.ToString().ToLower()}}
          ));
          """;
}

public static class StringHelper
{
    extension(string str)
    {
        public string Lower() => char.ToLower(str[0]) + str.Substring(1);
        public string Upper() => char.ToUpper(str[0]) + str.Substring(1);
    }
}