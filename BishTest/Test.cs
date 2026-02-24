global using BishRuntime;
global using BishBytecode;
global using FluentAssertions;
global using Bytecodes = BishBytecode.Bytecodes;
using FluentAssertions.Specialized;

namespace BishTest;

public class Test
{
    protected static Action Action(Action action) => action;

    protected static BishInt I(int x) => new(x);
    protected static BishNum N(double x) => new(x);
    protected static BishString S(string s) => new(s);
    protected static BishBool B(bool b) => new(b);
    protected static BishList L(params List<BishObject> list) => new(list);

    protected static BishRange R(params int[] args) =>
        (BishRange)BishRange.StaticType.CreateInstance(args.Select(I).ToList<BishObject>());

    protected static BishType T(string name, params List<BishType> parents) => new(name, parents);

    protected static BishNull Null => BishNull.Instance;

    protected static BishFrame Compile(string code, BishScope scope) => BishCompiler.BishCompiler.Compile(code, scope);
}

public static class BishExceptionAssertion
{
    public static ExceptionAssertions<BishException> Excepts<TDelegate, TAssertions>(
        this DelegateAssertions<TDelegate, TAssertions> assertions, BishType? errorType = null)
        where TDelegate : Delegate
        where TAssertions : DelegateAssertions<TDelegate, TAssertions>
        => assertions.Throw<BishException>().Where(ex => ex.Error.Type.CanAssignTo(errorType ?? BishError.StaticType));
}