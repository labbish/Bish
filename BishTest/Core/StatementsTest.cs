namespace BishTest.Core;

public class StatementsTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestIf()
    {
        Execute("x:=0;if(true)x=1;");
        ExpectResult("x", I(1));
        Execute("x:=0;if(false)x=1;");
        ExpectResult("x", I(0));
        Execute("x:=0;if(true){x=1;y:=2;}else{y:=3;}");
        ExpectResult("x", I(1));
        Action(() => Execute("y;")).Should().Excepts(BishError.AttributeErrorType);
        Execute("x:=0;if(false){x=1;y:=2;}else{y:=3;}");
        ExpectResult("x", I(0));
        Execute("if(y:=true){};");
        Action(() => Execute("y;")).Should().Excepts(BishError.AttributeErrorType);
    }

    [Fact]
    public void TestWhile()
    {
        Execute("x:=1;i:=4;while(i>0){x*=i;i-=1};");
        ExpectResult("x", I(24));
        Execute("x:=0;while(false)x=1;");
        ExpectResult("x", I(0));
        Execute("x:=1;i:=4;do{x*=i;i-=1;}while(i>0);");
        ExpectResult("x", I(24));
        Execute("x:=0;do x=1 while(false);");
        ExpectResult("x", I(1));
        Execute("while(y:=false){};do{}while(y:=false);");
        Action(() => Execute("y;")).Should().Excepts(BishError.AttributeErrorType);
    }

    [Fact]
    public void TestFor()
    {
        Execute("x:=1;for(i:range(1,5))x*=i;");
        ExpectResult("x", I(24));
        Action(() => Execute("i;")).Should().Excepts(BishError.AttributeErrorType);
    }

    [Theory]
    [InlineData(10, 4)]
    [InlineData(100, 25)]
    [InlineData(1000, 168)]
    public void TestStatements(int n, int primes)
    {
        var code = $$"""
                     s := 0;
                     for (n: range(2, {{n + 1}})) {
                         prime := true;
                         for (i: range(2, num.sqrt(n).floor() + 1))
                             if (n % i == 0)
                                 prime = false;
                         if (prime) s += 1;
                     };
                     """;
        Execute(code);
        ExpectResult("s", I(primes));
    }

    [Fact]
    public void TestBreak()
    {
        Action(() => Compile("break;")).Should().Throw();
        Execute("i:=0;while(true){i+=1;if(i==3)break;};");
        ExpectResult("i", I(3));
        Execute("i:=0;do{i+=1;if(i==3)break;}while(true);");
        ExpectResult("i", I(3));
        Execute("x:=0;for(i:range(0,100)){x=i;if(i==3)break;};");
        ExpectResult("x", I(3));
    }

    [Fact]
    public void TestContinue()
    {
        Action(() => Compile("continue;")).Should().Throw();
        Execute("x:=1;i:=5;while(i>1){i-=1;if(i==2)continue;x*=i;};");
        ExpectResult("x", I(12));
        Execute("x:=1;i:=5;do{i-=1;if(i==2)continue;x*=i;}while(i>1);");
        ExpectResult("x", I(12));
        Execute("x:=1;for(i:range(1,5)){if(i==2)continue;x*=i;};");
        ExpectResult("x", I(12));
    }

    [Fact]
    public void TestTagged()
    {
        Execute("x:=0;out:for(i:range(5))for(j:range(5)){x+=i+j;if(i==3&&j==2)break out};");
        ExpectResult("x", I(57));
        Execute("x:=0;out:for(i:range(5))for(j:range(5)){x+=i+j;if(i==3&&j==2)continue out};");
        ExpectResult("x", I(87));
    }

    [Fact]
    public void TestForDeconstruct()
    {
        Execute("s:='';for([i,c]:list('abcde').iter().entries)s+=i*c;");
        // ReSharper disable once StringLiteralTypo
        ExpectResult("s", S("bccdddeeee"));
    }
}