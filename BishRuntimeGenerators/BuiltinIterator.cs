namespace BishRuntimeGenerators;

public record BuiltinIterator(string Type, string? Namespace) : IBuiltinStuff
{
    public string Code =>
        $"BishBuiltinIteratorBinder.Bind({Type}.StaticType, " +
        $"obj => obj.As<{Type}>(\"self\").Next(), {(Type == "BishIterator").ToString().ToLower()});";
}