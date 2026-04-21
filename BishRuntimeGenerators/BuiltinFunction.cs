namespace BishRuntimeGenerators;

public record BuiltinFunctionArg(string Type, string Name, bool IsDefault, bool IsRest)
{
    public override string ToString() =>
        $"""new BishArg("{Name}", {Type}.StaticType, {(IsDefault ? "new DefaultNull()" : "null")}, {IsRest.ToString().ToLower()})""";
}

public record BuiltinFunction(
    string Type,
    string? Prefix,
    string Name,
    BuiltinFunctionArg[] Args,
    string? Tag,
    bool IsVoid,
    bool IsNullable,
    string? Namespace)
{
    public string FullName => (Prefix is null ? "" : $"{Prefix}_") + char.ToLower(Name[0]) + Name.Substring(1);

    public override string ToString() =>
        string.Join("\n",
            $$"""
                  {{Type}}.StaticType.DefMember("{{FullName}}", new BishFunc("{{FullName}}", 
                      [{{string.Join(", ", Args.Select(a => a.ToString()))}}],
                      raw_arg_list => {
                          {{string.Join(" ", Args.Select((arg, i) => $"var raw_{arg.Name} = raw_arg_list[{i}];"))}}
                          {{string.Join(" ", Args.Select(arg =>
                              $"var {arg.Name} = {(arg.IsDefault ? $"raw_{arg.Name} is DefaultNull ? null : " : "")}({arg.Type})raw_{arg.Name};"))}}
                          {{(IsVoid ? "" : "return ")}}{{Type}}.{{Name}}({{string.Join(", ", Args.Select(arg => arg.Name))}}){{(IsNullable ? " ?? (BishObject)BishNull.Instance" : "")}};
                          {{(IsVoid ? "return BishNull.Instance;" : "")}}
                      },
                      "{{Tag ?? "null"}}"
                  ));
                  """.Split('\n').Select(line => "\t\t" + line));
}