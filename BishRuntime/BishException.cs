namespace BishRuntime;

public class BishException(string? message = null, BishException? inner = null) : Exception(message, inner); // TODO

public class BishArgumentException(string? message = null, BishException? inner = null) : BishException(message, inner)
{
    public static BishArgumentException General(string argName) => new($"Failed to match argument {argName}");
    
    public static BishArgumentException OfDefineDefault(int pos, BishArg arg) => 
        new($"Required argument {arg.Name} at position {pos} cannot be after optional parameters");
    
    public static BishArgumentException OfDefineRepeat(string name) => 
        new($"Trying to define function with repeated argument {name}");

    public static BishArgumentException OfBind(BishMethod method, BishObject self) =>
        new($"Cannot bind {self} to {method} because {method} takes no argument");

    public static BishArgumentException OfCount(int min, int max, int got) =>
        new($"Wrong argument count: expected {(min == max ? min : $"{min}~{max}")}, got {got}");
    
    public static BishArgumentException OfType(BishObject obj, BishType type) => 
        new("Wrong argument type", new BishTypeException(obj, type));
}

public class BishTypeException(BishObject obj, BishType type)
    : BishException($"Wrong type: expected {type}, got {obj.Type}")
{
    public BishObject Obj => obj;
    public BishType Type => type;
}

public class BishNoSuchMemberException(BishObject obj, string name)
    : BishException($"No such member: {name} on object {obj}")
{
    public BishObject Obj => obj;
    public string Name => name;
}

public class BishNotCallableException(BishObject obj) : BishException($"{obj} is not callable")
{
    public BishObject Obj => obj;
}

public class BishNullAccessException(string name) : BishException($"Accessing member {name} on null")
{
    public string Name => name;
}