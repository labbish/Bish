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
        BishOperator.Call("op_Eq", [new BishInt(1), new BishNum(1)]).Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Eq", [new BishNum(1), new BishInt(1)]).Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Eq", [new BishInt(1), new BishNum(1.1)]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Eq", [new BishObject(), new BishObject()]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Eq", [new BishInt(1), new BishString("")]).Should().BeEquivalentTo(new BishBool(false));
    }

    [Fact]
    public void TestAutoCompare()
    {
        BishOperator.Call("op_Neq", [new BishInt(1), new BishInt(1)]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Neq", [new BishInt(1), new BishInt(2)]).Should().BeEquivalentTo(new BishBool(true));

        BishOperator.Call("op_Lt", [new BishInt(1), new BishInt(2)]).Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Lt", [new BishInt(2), new BishInt(2)]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Lt", [new BishInt(2), new BishInt(1)]).Should().BeEquivalentTo(new BishBool(false));

        BishOperator.Call("op_Le", [new BishInt(1), new BishInt(2)]).Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Le", [new BishInt(2), new BishInt(2)]).Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Le", [new BishInt(2), new BishInt(1)]).Should().BeEquivalentTo(new BishBool(false));

        BishOperator.Call("op_Gt", [new BishInt(1), new BishInt(2)]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Gt", [new BishInt(2), new BishInt(2)]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Gt", [new BishInt(2), new BishInt(1)]).Should().BeEquivalentTo(new BishBool(true));

        BishOperator.Call("op_Ge", [new BishInt(1), new BishInt(2)]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Ge", [new BishInt(2), new BishInt(2)]).Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Ge", [new BishInt(2), new BishInt(1)]).Should().BeEquivalentTo(new BishBool(true));
    }
}