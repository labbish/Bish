namespace BishTest.Runtime;

public class BuiltinsTest : Test
{
    [Fact]
    public void TestInt()
    {
        BishInt.StaticType.CreateInstance([]).Should().BeEquivalentTo(new BishInt(0));
        BishInt.StaticType.CreateInstance([new BishInt(3)]).Should().BeEquivalentTo(new BishInt(3));

        BishOperator.Call("op_Pos", [new BishInt(1)]).Should().BeEquivalentTo(new BishInt(1));
        BishOperator.Call("op_Neg", [new BishInt(1)]).Should().BeEquivalentTo(new BishInt(-1));
        BishOperator.Call("op_Add", [new BishInt(3), new BishInt(2)]).Should().BeEquivalentTo(new BishInt(5));
        BishOperator.Call("op_Sub", [new BishInt(3), new BishInt(2)]).Should().BeEquivalentTo(new BishInt(1));
        BishOperator.Call("op_Mul", [new BishInt(3), new BishInt(2)]).Should().BeEquivalentTo(new BishInt(6));
        BishOperator.Call("op_Div", [new BishInt(3), new BishInt(2)]).Should().BeEquivalentTo(new BishNum(1.5));
        BishOperator.Call("op_Mod", [new BishInt(3), new BishInt(2)]).Should().BeEquivalentTo(new BishInt(1));
        BishOperator.Call("op_Mod", [new BishInt(-3), new BishInt(2)]).Should().BeEquivalentTo(new BishInt(-1));
        Action(() => BishOperator.Call("op_Mod", [new BishInt(3), new BishInt(0)])).Should().Throw();
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
        BishOperator.Call("op_Bool", [new BishInt(0)]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Bool", [new BishInt(3)]).Should().BeEquivalentTo(new BishBool(true));
    }

    [Fact]
    public void TestNum()
    {
        BishNum.StaticType.CreateInstance([]).Should().BeEquivalentTo(new BishNum(0));
        BishNum.StaticType.CreateInstance([new BishNum(3)]).Should().BeEquivalentTo(new BishNum(3));

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
        BishOperator.Call("op_Bool", [new BishNum(0)]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Bool", [new BishNum(3)]).Should().BeEquivalentTo(new BishBool(true));

        BishNum.StaticType.GetMember("E").Should().BeOfType<BishNum>()
            .Which.Value.Should().BeApproximately(Math.E, 1e-5);
        BishNum.StaticType.GetMember("PI").Should().BeOfType<BishNum>()
            .Which.Value.Should().BeApproximately(Math.PI, 1e-5);
    }

    [Fact]
    public void TestBool()
    {
        BishBool.StaticType.CreateInstance([]).Should().BeEquivalentTo(new BishBool(false));
        BishBool.StaticType.CreateInstance([new BishBool(true)]).Should().BeEquivalentTo(new BishBool(true));

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

        BishOperator.Call("op_Bool", [new BishBool(false)]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Bool", [new BishBool(true)]).Should().BeEquivalentTo(new BishBool(true));
    }

    [Fact]
    public void TestString()
    {
        BishString.StaticType.CreateInstance([]).Should().BeEquivalentTo(new BishString(""));
        BishString.StaticType.CreateInstance([new BishString("abc")]).Should().BeEquivalentTo(new BishString("abc"));

        BishOperator.Call("op_Add", [new BishString("a"), new BishString("bc")])
            .Should().BeEquivalentTo(new BishString("abc"));
        BishOperator.Call("op_Mul", [new BishString("a"), new BishInt(3)])
            .Should().BeEquivalentTo(new BishString("aaa"));
        BishOperator.Call("op_Mul", [new BishInt(3), new BishString("a")])
            .Should().BeEquivalentTo(new BishString("aaa"));
        new BishString("abc").GetMember("toString").Call([]).Should().BeEquivalentTo(new BishString("abc"));
        BishOperator.Call("op_Eq", [new BishString("a"), new BishString("a")])
            .Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_Eq", [new BishString("a"), new BishString("b")])
            .Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Bool", [new BishString("")]).Should().BeEquivalentTo(new BishBool(false));
        BishOperator.Call("op_Bool", [new BishString("a")]).Should().BeEquivalentTo(new BishBool(true));
        BishOperator.Call("op_GetIndex", [new BishString("abc"), new BishInt(1)])
            .Should().BeEquivalentTo(new BishString("b"));
        new BishString("abc").GetMember("length").Should().BeEquivalentTo(new BishInt(3));

        var iter = BishOperator.Call("op_Iter", [new BishString("abc")]);
        iter.GetMember("next").Call([]).Should().BeEquivalentTo(new BishString("a"));
        iter.GetMember("next").Call([]).Should().BeEquivalentTo(new BishString("b"));
        iter.GetMember("next").Call([]).Should().BeEquivalentTo(new BishString("c"));
        Action(() => iter.GetMember("next").Call([])).Should().Excepts(BishError.IteratorStopType);
    }

    [Fact]
    public void TestRange()
    {
        var range = BishRange.StaticType.CreateInstance([new BishInt(1), new BishInt(10), new BishInt(3)]);
        range.GetMember("next").Call([]).Should().BeEquivalentTo(new BishInt(1));
        range.GetMember("next").Call([]).Should().BeEquivalentTo(new BishInt(4));
        range.GetMember("next").Call([]).Should().BeEquivalentTo(new BishInt(7));
        Action(() => range.GetMember("next").Call([])).Should().Excepts(BishError.IteratorStopType);
        range.GetMember("start").Should().BeEquivalentTo(new BishInt(1));
        range.GetMember("end").Should().BeEquivalentTo(new BishInt(10));
        range.GetMember("step").Should().BeEquivalentTo(new BishInt(3));
        BishRange.StaticType.CreateInstance([new BishInt(1), new BishInt(10), new BishInt(1)]).Should()
            .BeEquivalentTo(BishRange.StaticType.CreateInstance([new BishInt(1), new BishInt(10)]));
    }
}