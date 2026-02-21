namespace BishRuntimeTest;

public class BuiltinsTest : Test
{
    [Fact]
    public void TestInt()
    {
        BishInt.StaticType.CreateInstance([]).Should().BeEquivalentTo(new BishInt(0));
        BishOperator.Call("op_Pos", [new BishInt(1)]).Should().BeEquivalentTo(new BishInt(1));
        BishOperator.Call("op_Neg", [new BishInt(1)]).Should().BeEquivalentTo(new BishInt(-1));
        BishOperator.Call("op_Add", [new BishInt(3), new BishInt(2)]).Should().BeEquivalentTo(new BishInt(5));
        BishOperator.Call("op_Sub", [new BishInt(3), new BishInt(2)]).Should().BeEquivalentTo(new BishInt(1));
        BishOperator.Call("op_Mul", [new BishInt(3), new BishInt(2)]).Should().BeEquivalentTo(new BishInt(6));
        BishOperator.Call("op_Div", [new BishInt(3), new BishInt(2)]).Should().BeEquivalentTo(new BishNum(1.5));
        BishOperator.Call("op_Mod", [new BishInt(3), new BishInt(2)]).Should().BeEquivalentTo(new BishInt(1));
        BishOperator.Call("op_Mod", [new BishInt(-3), new BishInt(2)]).Should().BeEquivalentTo(new BishInt(-1));
        BishOperator.Call("op_Pow", [new BishInt(3), new BishInt(2)]).Should().BeEquivalentTo(new BishNum(9));
        new BishInt(3).GetMember("abs").Call([]).Should().BeEquivalentTo(new BishInt(3));
        new BishInt(0).GetMember("abs").Call([]).Should().BeEquivalentTo(new BishInt(0));
        new BishInt(-3).GetMember("abs").Call([]).Should().BeEquivalentTo(new BishInt(3));
        new BishInt(3).GetMember("sign").Call([]).Should().BeEquivalentTo(new BishInt(1));
        new BishInt(0).GetMember("sign").Call([]).Should().BeEquivalentTo(new BishInt(0));
        new BishInt(-3).GetMember("sign").Call([]).Should().BeEquivalentTo(new BishInt(-1));
        new BishInt(3).GetMember("toString").Call([]).Should().BeEquivalentTo(new BishString("3"));
        BishOperator.Call("op_Eq", [new BishInt(3), new BishInt(3)]).Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Eq", [new BishInt(3), new BishInt(2)]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Cmp", [new BishInt(3), new BishInt(2)]).Should().BeOfType<BishInt>()
            .Which.Value.Should().BePositive();
        BishOperator.Call("op_Cmp", [new BishInt(3), new BishInt(3)]).Should().BeEquivalentTo(new BishInt(0));
        BishOperator.Call("op_Cmp", [new BishInt(2), new BishInt(3)]).Should().BeOfType<BishInt>()
            .Which.Value.Should().BeNegative();
    }

    [Fact]
    public void TestNum()
    {
        BishNum.StaticType.CreateInstance([]).Should().BeEquivalentTo(new BishNum(0));
        BishOperator.Call("op_Pos", [new BishNum(1)]).Should().BeEquivalentTo(new BishNum(1));
        BishOperator.Call("op_Neg", [new BishNum(1)]).Should().BeEquivalentTo(new BishNum(-1));
        BishOperator.Call("op_Add", [new BishNum(3), new BishNum(2)]).Should().BeEquivalentTo(new BishNum(5));
        BishOperator.Call("op_Sub", [new BishNum(3), new BishNum(2)]).Should().BeEquivalentTo(new BishNum(1));
        BishOperator.Call("op_Mul", [new BishNum(3), new BishNum(2)]).Should().BeEquivalentTo(new BishNum(6));
        BishOperator.Call("op_Div", [new BishNum(3), new BishNum(2)]).Should().BeEquivalentTo(new BishNum(1.5));
        BishOperator.Call("op_Mod", [new BishNum(3), new BishNum(2)]).Should().BeEquivalentTo(new BishNum(1));
        BishOperator.Call("op_Mod", [new BishNum(-3), new BishNum(2)]).Should().BeEquivalentTo(new BishNum(-1));
        BishOperator.Call("op_Pow", [new BishNum(3), new BishNum(2)]).Should().BeEquivalentTo(new BishNum(9));
        new BishNum(3).GetMember("abs").Call([]).Should().BeEquivalentTo(new BishNum(3));
        new BishNum(0).GetMember("abs").Call([]).Should().BeEquivalentTo(new BishNum(0));
        new BishNum(-3).GetMember("abs").Call([]).Should().BeEquivalentTo(new BishNum(3));
        new BishNum(3).GetMember("sign").Call([]).Should().BeEquivalentTo(new BishInt(1));
        new BishNum(0).GetMember("sign").Call([]).Should().BeEquivalentTo(new BishInt(0));
        new BishNum(-3).GetMember("sign").Call([]).Should().BeEquivalentTo(new BishInt(-1));
        new BishNum(1.3).GetMember("floor").Call([]).Should().BeEquivalentTo(new BishInt(1));
        new BishNum(1.3).GetMember("ceil").Call([]).Should().BeEquivalentTo(new BishInt(2));
        new BishNum(1.3).GetMember("round").Call([]).Should().BeEquivalentTo(new BishInt(1));
        new BishNum(1.7).GetMember("round").Call([]).Should().BeEquivalentTo(new BishInt(2));
        new BishNum(3).GetMember("toString").Call([]).Should().BeEquivalentTo(new BishString("3"));
        BishOperator.Call("op_Eq", [new BishNum(3), new BishNum(3)]).Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Eq", [new BishNum(3), new BishNum(2)]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Cmp", [new BishNum(3), new BishNum(2)]).Should().BeOfType<BishInt>()
            .Which.Value.Should().BePositive();
        BishOperator.Call("op_Cmp", [new BishNum(3), new BishNum(3)]).Should().BeEquivalentTo(new BishInt(0));
        BishOperator.Call("op_Cmp", [new BishNum(2), new BishNum(3)]).Should().BeOfType<BishInt>()
            .Which.Value.Should().BeNegative();
    }

    [Fact]
    public void TestBool()
    {
        BishBool.StaticType.CreateInstance([]).Should().BeEquivalentTo(new BishBool(false));
        
        BishOperator.Call("op_Invert", [new BishBool(false)]).Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Invert", [new BishBool(true)]).Should().BeEquivalentTo(new BishBool(false));
        
        BishOperator.Call("op_Eq", [new BishBool(false), new BishBool(false)]).Should()
            .BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Eq", [new BishBool(false), new BishBool(true)]).Should()
            .BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Eq", [new BishBool(true), new BishBool(false)]).Should()
            .BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Eq", [new BishBool(true), new BishBool(true)]).Should()
            .BeEquivalentTo(new BishBool(true));
    }
}