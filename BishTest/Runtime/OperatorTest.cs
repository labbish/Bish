namespace BishTest.Runtime;

public class OperatorTest : Test
{
    // This is somehow duplicate with tests of builtin int and num types
    [Fact]
    public void TestOperatorTryCall()
    {
        BishOperator.TryCall("op_add", [I(1), I(2)]).Should().BeEquivalentTo(I(3));
        BishOperator.TryCall("op_add", [I(1), N(2.5)]).Should().BeEquivalentTo(N(3.5));
        BishOperator.TryCall("op_add", [N(1.5), I(2)]).Should().BeEquivalentTo(N(3.5));
        BishOperator.TryCall("op_add", []).Should().BeNull();
        BishOperator.TryCall("op_add", [I(1)]).Should().BeNull();
        BishOperator.TryCall("op_add", [I(1), S("2")]).Should().BeNull();
    }

    [Fact]
    public void TestOperatorCall()
    {
        BishOperator.Call("op_add", [I(1), I(2)]).Should().BeEquivalentTo(I(3));
        BishOperator.Call("op_add", [I(1), N(2.5)]).Should().BeEquivalentTo(N(3.5));
        BishOperator.Call("op_add", [N(1.5), I(2)]).Should().BeEquivalentTo(N(3.5));
        Action(() => BishOperator.Call("op_add", [])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => BishOperator.Call("op_add", [I(1)])).Should().Excepts(BishError.ArgumentErrorType);
        Action(() => BishOperator.Call("op_add", [I(1), S("2")])).Should()
            .Excepts(BishError.ArgumentErrorType);
    }

    [Fact]
    public void TestEquality()
    {
        BishOperator.Call("op_eq", [I(1), I(1)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_eq", [I(1), I(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_eq", [I(1), N(1)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_eq", [N(1), I(1)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_eq", [I(1), N(1.1)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_eq", [new BishObject(), new BishObject()]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_eq", [I(1), S("")]).Should().BeEquivalentTo(B(false));
    }

    [Fact]
    public void TestAutoCompare()
    {
        BishOperator.Call("op_neq", [I(1), I(1)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_neq", [I(1), I(2)]).Should().BeEquivalentTo(B(true));

        BishOperator.Call("op_lt", [I(1), I(2)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_lt", [I(2), I(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_lt", [I(2), I(1)]).Should().BeEquivalentTo(B(false));

        BishOperator.Call("op_le", [I(1), I(2)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_le", [I(2), I(2)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_le", [I(2), I(1)]).Should().BeEquivalentTo(B(false));

        BishOperator.Call("op_gt", [I(1), I(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_gt", [I(2), I(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_gt", [I(2), I(1)]).Should().BeEquivalentTo(B(true));

        BishOperator.Call("op_ge", [I(1), I(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_ge", [I(2), I(2)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_ge", [I(2), I(1)]).Should().BeEquivalentTo(B(true));
    }
}