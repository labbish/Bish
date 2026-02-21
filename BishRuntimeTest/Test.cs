global using BishRuntime;
global using FluentAssertions;
using FluentAssertions.Specialized;

namespace BishRuntimeTest;

public class Test
{
    protected static Action Action(Action action) => action;
}

public static class BishExceptionAssertion
{
    public static void Excepts<TDelegate, TAssertions>(
        this DelegateAssertions<TDelegate, TAssertions> assertions, BishType errorType)
        where TDelegate : Delegate
        where TAssertions : DelegateAssertions<TDelegate, TAssertions>
        => assertions.Throw<BishException>().Where(ex => ex.Error.Type == errorType);
}