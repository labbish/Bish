namespace BishTest.Runtime;

public class BuiltinsTest : Test
{
    [Fact]
    public void TestInt()
    {
        BishInt.StaticType.CreateInstance([]).Should().BeEquivalentTo(I(0));
        BishInt.StaticType.CreateInstance([I(3)]).Should().BeEquivalentTo(I(3));

        BishOperator.Call("op_pos", [I(1)]).Should().BeEquivalentTo(I(1));
        BishOperator.Call("op_neg", [I(1)]).Should().BeEquivalentTo(I(-1));
        BishOperator.Call("op_add", [I(3), I(2)]).Should().BeEquivalentTo(I(5));
        BishOperator.Call("op_sub", [I(3), I(2)]).Should().BeEquivalentTo(I(1));
        BishOperator.Call("op_mul", [I(3), I(2)]).Should().BeEquivalentTo(I(6));
        BishOperator.Call("op_div", [I(3), I(2)]).Should().BeEquivalentTo(N(1.5));
        BishOperator.Call("op_mod", [I(3), I(2)]).Should().BeEquivalentTo(I(1));
        BishOperator.Call("op_mod", [I(-3), I(2)]).Should().BeEquivalentTo(I(-1));
        Action(() => BishOperator.Call("op_mod", [I(3), I(0)])).Should().Excepts();
        BishOperator.Call("op_pow", [I(3), I(2)]).Should().BeEquivalentTo(N(9));

        I(3).GetMember("abs").Call([]).Should().BeEquivalentTo(I(3));
        I(0).GetMember("abs").Call([]).Should().BeEquivalentTo(I(0));
        I(-3).GetMember("abs").Call([]).Should().BeEquivalentTo(I(3));
        I(3).GetMember("sign").Call([]).Should().BeEquivalentTo(I(1));
        I(0).GetMember("sign").Call([]).Should().BeEquivalentTo(I(0));
        I(-3).GetMember("sign").Call([]).Should().BeEquivalentTo(I(-1));
        I(3).GetMember("toString").Call([]).Should().BeEquivalentTo(S("3"));

        BishOperator.Call("op_eq", [I(3), I(3)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_eq", [I(3), I(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_cmp", [I(3), I(2)]).Should().BeOfType<BishInt>().Which.Value.Should().BePositive();
        BishOperator.Call("op_cmp", [I(3), I(3)]).Should().BeEquivalentTo(I(0));
        BishOperator.Call("op_cmp", [I(2), I(3)]).Should().BeOfType<BishInt>().Which.Value.Should().BeNegative();
        BishOperator.Call("op_bool", [I(0)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_bool", [I(3)]).Should().BeEquivalentTo(B(true));
    }

    [Fact]
    public void TestNum()
    {
        BishNum.StaticType.CreateInstance([]).Should().BeEquivalentTo(N(0));
        BishNum.StaticType.CreateInstance([N(3)]).Should().BeEquivalentTo(N(3));

        BishOperator.Call("op_pos", [N(1)]).Should().BeEquivalentTo(N(1));
        BishOperator.Call("op_neg", [N(1)]).Should().BeEquivalentTo(N(-1));
        BishOperator.Call("op_add", [N(3), N(2)]).Should().BeEquivalentTo(N(5));
        BishOperator.Call("op_sub", [N(3), N(2)]).Should().BeEquivalentTo(N(1));
        BishOperator.Call("op_mul", [N(3), N(2)]).Should().BeEquivalentTo(N(6));
        BishOperator.Call("op_div", [N(3), N(2)]).Should().BeEquivalentTo(N(1.5));
        BishOperator.Call("op_mod", [N(3), N(2)]).Should().BeEquivalentTo(N(1));
        BishOperator.Call("op_mod", [N(-3), N(2)]).Should().BeEquivalentTo(N(-1));
        BishOperator.Call("op_pow", [N(3), N(2)]).Should().BeEquivalentTo(N(9));

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

        BishOperator.Call("op_eq", [N(3), N(3)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_eq", [N(3), N(2)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_cmp", [N(3), N(2)]).Should().BeOfType<BishInt>().Which.Value.Should().BePositive();
        BishOperator.Call("op_cmp", [N(3), N(3)]).Should().BeEquivalentTo(I(0));
        BishOperator.Call("op_cmp", [N(2), N(3)]).Should().BeOfType<BishInt>().Which.Value.Should().BeNegative();
        BishOperator.Call("op_bool", [N(0)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_bool", [N(3)]).Should().BeEquivalentTo(B(true));

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

        BishOperator.Call("op_invert", [B(false)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_invert", [B(true)]).Should().BeEquivalentTo(B(false));

        BishOperator.Call("op_eq", [B(false), B(false)]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_eq", [B(false), B(true)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_eq", [B(true), B(false)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_eq", [B(true), B(true)]).Should().BeEquivalentTo(B(true));
        
        BishOperator.Call("op_bool", [B(false)]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_bool", [B(true)]).Should().BeEquivalentTo(B(true));
    }

    [Fact]
    public void TestString()
    {
        BishString.StaticType.CreateInstance([]).Should().BeEquivalentTo(S(""));
        BishString.StaticType.CreateInstance([S("abc")]).Should().BeEquivalentTo(S("abc"));

        BishOperator.Call("op_add", [S("a"), S("bc")]).Should().BeEquivalentTo(S("abc"));
        BishOperator.Call("op_mul", [S("a"), I(3)]).Should().BeEquivalentTo(S("aaa"));
        BishOperator.Call("op_mul", [I(3), S("a")]).Should().BeEquivalentTo(S("aaa"));
        S("abc").GetMember("toString").Call([]).Should().BeEquivalentTo(S("abc"));
        BishOperator.Call("op_eq", [S("a"), S("a")]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_eq", [S("a"), S("b")]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_bool", [S("")]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_bool", [S("a")]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_getIndex", [S("abc"), I(1)]).Should().BeEquivalentTo(S("b"));
        BishOperator.Call("op_getIndex", [S("abc"), I(-1)]).Should().BeEquivalentTo(S("c"));
        Action(() => BishOperator.Call("op_getIndex", [S("abc"), I(3)])).Should().Excepts();
        Action(() => BishOperator.Call("op_getIndex", [S("abc"), I(-4)])).Should().Excepts();
        S("abc").GetMember("length").Should().BeEquivalentTo(I(3));

        var iter = BishOperator.Call("op_iter", [S("abc")]);
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

        var reversed = BishRange.StaticType.CreateInstance([I(10), I(1), I(-3)]);
        reversed.GetMember("next").Call([]).Should().BeEquivalentTo(I(10));
        reversed.GetMember("next").Call([]).Should().BeEquivalentTo(I(7));
        reversed.GetMember("next").Call([]).Should().BeEquivalentTo(I(4));
        Action(() => reversed.GetMember("next").Call([])).Should().Excepts(BishError.IteratorStopType);

        BishRange.StaticType.CreateInstance([I(1), I(10), I(1)]).Should()
            .BeEquivalentTo(BishRange.StaticType.CreateInstance([I(1), I(10)]));
        BishRange.StaticType.CreateInstance([I(0), I(10), I(1)]).Should()
            .BeEquivalentTo(BishRange.StaticType.CreateInstance([I(10)]));

        Action(() => BishRange.StaticType.CreateInstance([I(0), I(0), I(0)])).Should()
            .Excepts(BishError.ArgumentErrorType);
    }

    [Fact]
    public void TestList()
    {
        BishList.StaticType.CreateInstance([]).Should().BeEquivalentTo(new BishList([]));

        var a = I(0);
        var b = S("x");
        var c = B(true);
        BishOperator.Call("op_add", [L(a, b), L(c)]).Should().BeEquivalentTo(L(a, b, c));
        BishOperator.Call("op_mul", [L(a, b), I(3)]).Should().BeEquivalentTo(L(a, b, a, b, a, b));
        BishOperator.Call("op_mul", [I(3), L(a, b)]).Should().BeEquivalentTo(L(a, b, a, b, a, b));
        BishOperator.Call("op_eq", [L(a, L(b, c)), L(a, L(c, c))]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_eq", [L(a, L(b, c)), L(a, L(b, c))]).Should().BeEquivalentTo(B(true));
        BishOperator.Call("op_bool", [L()]).Should().BeEquivalentTo(B(false));
        BishOperator.Call("op_bool", [L(a)]).Should().BeEquivalentTo(B(true));

        var l = L(a, c, b);
        BishOperator.Call("op_getIndex", [l, I(0)]).Should().BeEquivalentTo(a);
        BishOperator.Call("op_getIndex", [l, I(-1)]).Should().BeEquivalentTo(b);
        BishOperator.Call("op_setIndex", [l, I(1), b]); // [a, b, b]
        BishOperator.Call("op_delIndex", [l, I(2)]); // [a, b]
        l.GetMember("add").Call([c]); // [a, b, c]
        l.Should().BeEquivalentTo(L(a, b, c));
        l.GetMember("length").Should().BeEquivalentTo(I(3));

        Action(() => BishOperator.Call("op_getIndex", [l, I(3)])).Should().Excepts();
        Action(() => BishOperator.Call("op_delIndex", [l, I(-4)])).Should().Excepts();

        var iter = BishOperator.Call("op_iter", [l]);
        iter.GetMember("next").Call([]).Should().BeEquivalentTo(a);
        iter.GetMember("next").Call([]).Should().BeEquivalentTo(b);
        iter.GetMember("next").Call([]).Should().BeEquivalentTo(c);
        Action(() => iter.GetMember("next").Call([])).Should().Excepts(BishError.IteratorStopType);
        l.Should().BeEquivalentTo(L(a, b, c));

        BishList.StaticType.CreateInstance([BishRange.StaticType.CreateInstance([I(5)])]).Should()
            .BeEquivalentTo(L(I(0), I(1), I(2), I(3), I(4)));
    }

    [Fact]
    public void TestRangeIndex()
    {
        var s = S("0123456789");
        BishOperator.Call("op_getIndex", [s, R(2, 7)]).Should().BeEquivalentTo(S("23456"));
        BishOperator.Call("op_getIndex", [s, R(5, -2)]).Should().BeEquivalentTo(S("567"));
        BishOperator.Call("op_getIndex", [s, R(2, -1, 2)]).Should().BeEquivalentTo(S("2468"));
        BishOperator.Call("op_getIndex", [s, R(8, 3, -1)]).Should().BeEquivalentTo(S("87654"));

        var l = L(I(0), I(1), I(2), I(3), I(4), I(5), I(6), I(7), I(8), I(9));
        BishOperator.Call("op_getIndex", [l, R(2, 7)]).Should().BeEquivalentTo(L(I(2), I(3), I(4), I(5), I(6)));
        BishOperator.Call("op_getIndex", [l, R(5, -2)]).Should().BeEquivalentTo(L(I(5), I(6), I(7)));
        BishOperator.Call("op_getIndex", [l, R(2, -1, 2)]).Should().BeEquivalentTo(L(I(2), I(4), I(6), I(8)));
        BishOperator.Call("op_getIndex", [l, R(8, 3, -1)]).Should().BeEquivalentTo(L(I(8), I(7), I(6), I(5), I(4)));
        
        BishOperator.Call("op_setIndex", [l, R(2, 7), L(Null, Null, Null)]).Should().BeEquivalentTo(L(Null, Null, Null));
        l.Should().BeEquivalentTo(L(I(0), I(1), Null, Null, Null, I(7), I(8), I(9)));
        BishOperator.Call("op_setIndex", [l, R(6, 0, -2), L(S("a"), S("b"), S("c"))]);
        l.Should().BeEquivalentTo(L(I(0), I(1), S("c"), Null, S("b"), I(7), S("a"), I(9)));
        
        BishOperator.Call("op_delIndex", [l, R(1, 8, 3)]).Should().BeEquivalentTo(L(I(1), S("b"), I(9)));
        l.Should().BeEquivalentTo(L(I(0), S("c"), Null, I(7), S("a")));
    }
}