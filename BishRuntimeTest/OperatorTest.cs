using BishRuntime;
using FluentAssertions;

namespace BishRuntimeTest;

public class OperatorTest : Test
{
    // This is somehow duplicate with tests of builtin int and num types
    [Fact]
    public void TestOperatorTryCall()
    {
        BishOperator.TryCall("op_Add", [new BishInt(1), new BishInt(2)]).Should().BeEquivalentTo(new BishInt(3));
        BishOperator.TryCall("op_Add", [new BishInt(1), new BishNum(2.5m)]).Should().BeEquivalentTo(new BishNum(3.5m));
        BishOperator.TryCall("op_Add", [new BishNum(1.5m), new BishInt(2)]).Should().BeEquivalentTo(new BishNum(3.5m));
        BishOperator.TryCall("op_Add", []).Should().BeNull();
        BishOperator.TryCall("op_Add", [new BishInt(1)]).Should().BeNull();
        BishOperator.TryCall("op_Add", [new BishInt(1), new BishString("2")]).Should().BeNull();
    }

    [Fact]
    public void TestOperatorCall()
    {
        BishOperator.Call("op_Add", [new BishInt(1), new BishInt(2)]).Should().BeEquivalentTo(new BishInt(3));
        BishOperator.Call("op_Add", [new BishInt(1), new BishNum(2.5m)]).Should().BeEquivalentTo(new BishNum(3.5m));
        BishOperator.Call("op_Add", [new BishNum(1.5m), new BishInt(2)]).Should().BeEquivalentTo(new BishNum(3.5m));
        Action(() => BishOperator.Call("op_Add", [])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => BishOperator.Call("op_Add", [new BishInt(1)])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => BishOperator.Call("op_Add", [new BishInt(1), new BishString("2")])).Should()
            .Excepts(BishError.ArgumentErrorType);
    }

    [Fact]
    public void TestEquality()
    {
        BishOperator.Call("op_Eq", [new BishInt(1), new BishInt(1)]).Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Eq", [new BishInt(1), new BishInt(2)]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Eq", [new BishInt(1), new BishNum(1m)]).Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Eq", [new BishNum(1m), new BishInt(1)]).Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Eq", [new BishInt(1), new BishNum(1.1m)]).Should().BeEquivalentTo(new BishBool(false));
    }
}