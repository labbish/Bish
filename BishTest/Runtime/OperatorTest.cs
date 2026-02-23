namespace BishTest.Runtime;

public class OperatorTest : Test
{
    // This is somehow duplicate with tests of builtin int and num types
    [Fact]
    public void TestOperatorTryCall()
    {
        BishOperator.TryCall("op_Add", [I(1), I(2)]).Should().BeEquivalentTo(I(3));
        BishOperator.TryCall("op_Add", [I(1), N(2.5)]).Should().BeEquivalentTo(N(3.5));
        BishOperator.TryCall("op_Add", [N(1.5), I(2)]).Should().BeEquivalentTo(N(3.5));
        BishOperator.TryCall("op_Add", []).Should().BeNull();
        BishOperator.TryCall("op_Add", [I(1)]).Should().BeNull();
        BishOperator.TryCall("op_Add", [I(1), S("2")]).Should().BeNull();
    }

    [Fact]
    public void TestOperatorCall()
    {
        BishOperator.Call("op_Add", [I(1), I(2)]).Should().BeEquivalentTo(I(3));
        BishOperator.Call("op_Add", [I(1), N(2.5)]).Should().BeEquivalentTo(N(3.5));
        BishOperator.Call("op_Add", [N(1.5), I(2)]).Should().BeEquivalentTo(N(3.5));
        Action(() => BishOperator.Call("op_Add", [])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => BishOperator.Call("op_Add", [I(1)])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => BishOperator.Call("op_Add", [I(1), S("2")])).Should()
            .Excepts(BishError.ArgumentErrorType);
    }

    [Fact]
    public void TestEquality()
    {
        BishOperator.Call("op_Eq", [I(1), I(1)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_Eq", [I(1), I(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Eq", [I(1), N(1)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_Eq", [N(1), I(1)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_Eq", [I(1), N(1.1)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Eq", [new BishObject(), new BishObject()]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Eq", [I(1), S("")]).Should().BeEquivalentTo(B(false));
    }

    [Fact]
    public void TestAutoCompare()
    {
        BishOperator.Call("op_Neq", [I(1), I(1)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Neq", [I(1), I(2)]).Should().BeEquivalentTo(B(true));

        BishOperator.Call("op_Lt", [I(1), I(2)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_Lt", [I(2), I(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Lt", [I(2), I(1)]).Should().BeEquivalentTo(B(false));

        BishOperator.Call("op_Le", [I(1), I(2)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_Le", [I(2), I(2)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_Le", [I(2), I(1)]).Should().BeEquivalentTo(B(false));

        BishOperator.Call("op_Gt", [I(1), I(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Gt", [I(2), I(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Gt", [I(2), I(1)]).Should().BeEquivalentTo(B(true));

        BishOperator.Call("op_Ge", [I(1), I(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Ge", [I(2), I(2)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_Ge", [I(2), I(1)]).Should().BeEquivalentTo(B(true));
    }
}