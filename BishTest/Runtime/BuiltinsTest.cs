namespace BishTest.Runtime;

public class BuiltinsTest : Test
{
    [Fact]
    public void TestInt()
    {
        BishInt.StaticType.CreateInstance([]).Should().BeEquivalentTo(I(0));
        BishInt.StaticType.CreateInstance([I(3)]).Should().BeEquivalentTo(I(3));

        BishOperator.Call("op_Pos", [I(1)]).Should().BeEquivalentTo(I(1));
        BishOperator.Call("op_Neg", [I(1)]).Should().BeEquivalentTo(I(-1));
        BishOperator.Call("op_Add", [I(3), I(2)]).Should().BeEquivalentTo(I(5));
        BishOperator.Call("op_Sub", [I(3), I(2)]).Should().BeEquivalentTo(I(1));
        BishOperator.Call("op_Mul", [I(3), I(2)]).Should().BeEquivalentTo(I(6));
        BishOperator.Call("op_Div", [I(3), I(2)]).Should().BeEquivalentTo(N(1.5));
        BishOperator.Call("op_Mod", [I(3), I(2)]).Should().BeEquivalentTo(I(1));
        BishOperator.Call("op_Mod", [I(-3), I(2)]).Should().BeEquivalentTo(I(-1));
        Action(() => BishOperator.Call("op_Mod", [I(3), I(0)])).Should().Excepts();
        BishOperator.Call("op_Pow", [I(3), I(2)]).Should().BeEquivalentTo(N(9));

        I(3).GetMember("abs").Call([]).Should().BeEquivalentTo(I(3));
        I(0).GetMember("abs").Call([]).Should().BeEquivalentTo(I(0));
        I(-3).GetMember("abs").Call([]).Should().BeEquivalentTo(I(3));
        I(3).GetMember("sign").Call([]).Should().BeEquivalentTo(I(1));
        I(0).GetMember("sign").Call([]).Should().BeEquivalentTo(I(0));
        I(-3).GetMember("sign").Call([]).Should().BeEquivalentTo(I(-1));
        I(3).GetMember("toString").Call([]).Should().BeEquivalentTo(S("3"));

        BishOperator.Call("op_Eq", [I(3), I(3)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_Eq", [I(3), I(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Cmp", [I(3), I(2)]).Should().BeOfType<BishInt>().Which.Value.Should().BePositive();
        BishOperator.Call("op_Cmp", [I(3), I(3)]).Should().BeEquivalentTo(I(0));
        BishOperator.Call("op_Cmp", [I(2), I(3)]).Should().BeOfType<BishInt>().Which.Value.Should().BeNegative();
        BishOperator.Call("op_Bool", [I(0)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Bool", [I(3)]).Should().BeEquivalentTo(B(true));
    }

    [Fact]
    public void TestNum()
    {
        BishNum.StaticType.CreateInstance([]).Should().BeEquivalentTo(N(0));
        BishNum.StaticType.CreateInstance([N(3)]).Should().BeEquivalentTo(N(3));

        BishOperator.Call("op_Pos", [N(1)]).Should().BeEquivalentTo(N(1));
        BishOperator.Call("op_Neg", [N(1)]).Should().BeEquivalentTo(N(-1));
        BishOperator.Call("op_Add", [N(3), N(2)]).Should().BeEquivalentTo(N(5));
        BishOperator.Call("op_Sub", [N(3), N(2)]).Should().BeEquivalentTo(N(1));
        BishOperator.Call("op_Mul", [N(3), N(2)]).Should().BeEquivalentTo(N(6));
        BishOperator.Call("op_Div", [N(3), N(2)]).Should().BeEquivalentTo(N(1.5));
        BishOperator.Call("op_Mod", [N(3), N(2)]).Should().BeEquivalentTo(N(1));
        BishOperator.Call("op_Mod", [N(-3), N(2)]).Should().BeEquivalentTo(N(-1));
        BishOperator.Call("op_Pow", [N(3), N(2)]).Should().BeEquivalentTo(N(9));

        N(3).GetMember("abs").Call([]).Should().BeEquivalentTo(N(3));
        N(0).GetMember("abs").Call([]).Should().BeEquivalentTo(N(0));
        N(-3).GetMember("abs").Call([]).Should().BeEquivalentTo(N(3));
        N(3).GetMember("sign").Call([]).Should().BeEquivalentTo(I(1));
        N(0).GetMember("sign").Call([]).Should().BeEquivalentTo(I(0));
        N(-3).GetMember("sign").Call([]).Should().BeEquivalentTo(I(-1));
        N(1.3).GetMember("floor").Call([]).Should().BeEquivalentTo(I(1));
        N(1.3).GetMember("ceil").Call([]).Should().BeEquivalentTo(I(2));
        N(1.3).GetMember("round").Call([]).Should().BeEquivalentTo(I(1));
        N(1.7).GetMember("round").Call([]).Should().BeEquivalentTo(I(2));
        N(3).GetMember("toString").Call([]).Should().BeEquivalentTo(S("3"));

        BishOperator.Call("op_Eq", [N(3), N(3)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_Eq", [N(3), N(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Cmp", [N(3), N(2)]).Should().BeOfType<BishInt>().Which.Value.Should().BePositive();
        BishOperator.Call("op_Cmp", [N(3), N(3)]).Should().BeEquivalentTo(I(0));
        BishOperator.Call("op_Cmp", [N(2), N(3)]).Should().BeOfType<BishInt>().Which.Value.Should().BeNegative();
        BishOperator.Call("op_Bool", [N(0)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Bool", [N(3)]).Should().BeEquivalentTo(B(true));

        BishNum.StaticType.GetMember("E").Should().BeOfType<BishNum>()
            .Which.Value.Should().BeApproximately(Math.E, 1e-5);
        BishNum.StaticType.GetMember("PI").Should().BeOfType<BishNum>()
            .Which.Value.Should().BeApproximately(Math.PI, 1e-5);
    }

    [Fact]
    public void TestBool()
    {
        BishBool.StaticType.CreateInstance([]).Should().BeEquivalentTo(B(false));
        BishBool.StaticType.CreateInstance([B(true)]).Should().BeEquivalentTo(B(true));

        BishOperator.Call("op_Invert", [B(false)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_Invert", [B(true)]).Should().BeEquivalentTo(B(false));

        BishOperator.Call("op_Eq", [B(false), B(false)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_Eq", [B(false), B(true)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Eq", [B(true), B(false)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Eq", [B(true), B(true)]).Should().BeEquivalentTo(B(true));

        BishOperator.Call("op_Bool", [B(false)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Bool", [B(true)]).Should().BeEquivalentTo(B(true));
    }

    [Fact]
    public void TestString()
    {
        BishString.StaticType.CreateInstance([]).Should().BeEquivalentTo(S(""));
        BishString.StaticType.CreateInstance([S("abc")]).Should().BeEquivalentTo(S("abc"));

        BishOperator.Call("op_Add", [S("a"), S("bc")]).Should().BeEquivalentTo(S("abc"));
        BishOperator.Call("op_Mul", [S("a"), I(3)]).Should().BeEquivalentTo(S("aaa"));
        BishOperator.Call("op_Mul", [I(3), S("a")]).Should().BeEquivalentTo(S("aaa"));
        S("abc").GetMember("toString").Call([]).Should().BeEquivalentTo(S("abc"));
        BishOperator.Call("op_Eq", [S("a"), S("a")]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_Eq", [S("a"), S("b")]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Bool", [S("")]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Bool", [S("a")]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_GetIndex", [S("abc"), I(1)]).Should().BeEquivalentTo(S("b"));
        BishOperator.Call("op_GetIndex", [S("abc"), I(-1)]).Should().BeEquivalentTo(S("c"));
        Action(() => BishOperator.Call("op_GetIndex", [S("abc"), I(3)])).Should().Excepts();
        Action(() => BishOperator.Call("op_GetIndex", [S("abc"), I(-4)])).Should().Excepts();
        S("abc").GetMember("length").Should().BeEquivalentTo(I(3));

        var iter = BishOperator.Call("op_Iter", [S("abc")]);
        iter.GetMember("next").Call([]).Should().BeEquivalentTo(S("a"));
        iter.GetMember("next").Call([]).Should().BeEquivalentTo(S("b"));
        iter.GetMember("next").Call([]).Should().BeEquivalentTo(S("c"));
        Action(() => iter.GetMember("next").Call([])).Should().Excepts(BishError.IteratorStopType);
    }

    [Fact]
    public void TestRange()
    {
        var range = BishRange.StaticType.CreateInstance([I(1), I(10), I(3)]);
        range.GetMember("next").Call([]).Should().BeEquivalentTo(I(1));
        range.GetMember("next").Call([]).Should().BeEquivalentTo(I(4));
        range.GetMember("next").Call([]).Should().BeEquivalentTo(I(7));
        Action(() => range.GetMember("next").Call([])).Should().Excepts(BishError.IteratorStopType);
        range.GetMember("start").Should().BeEquivalentTo(I(1));
        range.GetMember("end").Should().BeEquivalentTo(I(10));
        range.GetMember("step").Should().BeEquivalentTo(I(3));
        BishRange.StaticType.CreateInstance([I(1), I(10), I(1)]).Should()
            .BeEquivalentTo(BishRange.StaticType.CreateInstance([I(1), I(10)]));
    }

    [Fact]
    public void TestList()
    {
        BishList.StaticType.CreateInstance([]).Should().BeEquivalentTo(new BishList([]));
        
        var a = I(0);
        var b = S("x");
        var c = B(true);
        BishOperator.Call("op_Add", [L(a, b), L(c)]).Should().BeEquivalentTo(L(a, b, c));
        BishOperator.Call("op_Mul", [L(a, b), I(3)]).Should().BeEquivalentTo(L(a, b, a, b, a, b));
        BishOperator.Call("op_Mul", [I(3), L(a, b)]).Should().BeEquivalentTo(L(a, b, a, b, a, b));
        BishOperator.Call("op_Eq", [L(a, L(b, c)), L(a, L(c, c))]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Eq", [L(a, L(b, c)), L(a, L(b, c))]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_Bool", [L()]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_Bool", [L(a)]).Should().BeEquivalentTo(B(true));

        var l = L(a, c, b);
        BishOperator.Call("op_GetIndex", [l, I(0)]).Should().BeEquivalentTo(a);
        BishOperator.Call("op_GetIndex", [l, I(-1)]).Should().BeEquivalentTo(b);
        BishOperator.Call("op_SetIndex", [l, I(1), b]); // [a, b, b]
        BishOperator.Call("op_DelIndex", [l, I(2)]); // [a, b]
        l.GetMember("add").Call([c]); // [a, b, c]
        l.Should().BeEquivalentTo(L(a, b, c));
        l.GetMember("length").Should().BeEquivalentTo(I(3));

        Action(() => BishOperator.Call("op_GetIndex", [l, I(3)])).Should().Excepts();
        Action(() => BishOperator.Call("op_DelIndex", [l, I(-4)])).Should().Excepts();

        var iter = BishOperator.Call("op_Iter", [l]);
        iter.GetMember("next").Call([]).Should().BeEquivalentTo(a);
        iter.GetMember("next").Call([]).Should().BeEquivalentTo(b);
        iter.GetMember("next").Call([]).Should().BeEquivalentTo(c);
        Action(() => iter.GetMember("next").Call([])).Should().Excepts(BishError.IteratorStopType);
        l.Should().BeEquivalentTo(L(a, b, c));
    }
}