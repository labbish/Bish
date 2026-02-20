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
        BishOperator.TryCall("op_Add", [new BishInt(1), new BishNum(2.5)]).Should().BeEquivalentTo(new BishNum(3.5));
        BishOperator.TryCall("op_Add", [new BishNum(1.5), new BishInt(2)]).Should().BeEquivalentTo(new BishNum(3.5));
        BishOperator.TryCall("op_Add", []).Should().BeNull();
        BishOperator.TryCall("op_Add", [new BishInt(1)]).Should().BeNull();
        BishOperator.TryCall("op_Add", [new BishInt(1), new BishString("2")]).Should().BeNull();
    }

    [Fact]
    public void TestOperatorCall()
    {
        BishOperator.Call("op_Add", [new BishInt(1), new BishInt(2)]).Should().BeEquivalentTo(new BishInt(3));
        BishOperator.Call("op_Add", [new BishInt(1), new BishNum(2.5)]).Should().BeEquivalentTo(new BishNum(3.5));
        BishOperator.Call("op_Add", [new BishNum(1.5), new BishInt(2)]).Should().BeEquivalentTo(new BishNum(3.5));
        Action(() => BishOperator.Call("op_Add", [])).Should().Excepts(BishError.TypeErrorType);
        Action(() => BishOperator.Call("op_Add", [new BishInt(1)])).Should().Excepts(BishError.TypeErrorType);
        Action(() => BishOperator.Call("op_Add", [new BishInt(1), new BishString("2")])).Should()
            .Excepts(BishError.TypeErrorType);
    }
}