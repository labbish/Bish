namespace BishTest.Compiler;

public class StatementsTest(OptimizeInfoFixture fixture) : CompilerTest(fixture)
{
    [Fact]
    public void TestIf()
    {
        Execute("x:=0;if(true)x=1;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(1));
        Execute("x:=0;if(false)x=1;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(0));
        Execute("x:=0;if(true){x=1;y:=2;}else y:=3;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(1));
        Scope.TryGetVar("y").Should().BeNull();
        Execute("x:=0;if(false){x=1;y:=2;}else y:=3;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(0));
        Execute("if(y:=true);");
        Scope.TryGetVar("y").Should().BeNull();
    }

    [Fact]
    public void TestWhile()
    {
        Execute("x:=1;i:=4;while(i>0){x*=i;i-=1;}");
        Scope.GetVar("x").Should().BeEquivalentTo(I(24));
        Execute("x:=0;while(false)x=1;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(0));
        Execute("x:=1;i:=4;do{x*=i;i-=1;}while(i>0);");
        Scope.GetVar("x").Should().BeEquivalentTo(I(24));
        Execute("x:=0;do x=1;while(false);");
        Scope.GetVar("x").Should().BeEquivalentTo(I(1));
        Execute("while(y:=false);do;while(y:=false);");
        Scope.TryGetVar("y").Should().BeNull();
    }

    [Fact]
    public void TestFor()
    {
        Execute("x:=1;for(i:=1;i<5;i+=1)x*=i;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(24));
        Execute("x:=1;for(i:range(1,5))x*=i;");
        Scope.GetVar("x").Should().BeEquivalentTo(I(24));
        Scope.TryGetVar("i").Should().BeNull();
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
                     }
                     """;
        Execute(code);
        Scope.GetVar("s").Should().BeEquivalentTo(I(primes));
    }

    [Fact]
    public void TestBreak()
    {
        Action(() => Compile("break;")).Should().Throw();
        Execute("i:=0;while(true){i+=1;if(i==3)break;}");
        Scope.GetVar("i").Should().BeEquivalentTo(I(3));
        Execute("i:=0;do{i+=1;if(i==3)break;}while(true);");
        Scope.GetVar("i").Should().BeEquivalentTo(I(3));
        Execute("i:=0;for(i=0;true;i+=1)if(i==3)break;");
        Scope.GetVar("i").Should().BeEquivalentTo(I(3));
        Execute("i:=0;for(i::range(0,100))if(i==3)break;");
    }

    [Fact]
    public void TestContinue()
    {
        Action(() => Compile("continue;")).Should().Throw();
        Execute("x:=1;i:=5;while(i>1){i-=1;if(i==2)continue;x*=i;}");
        Scope.GetVar("x").Should().BeEquivalentTo(I(12));
        Execute("x:=1;i:=5;do{i-=1;if(i==2)continue;x*=i;}while(i>1);");
        Scope.GetVar("x").Should().BeEquivalentTo(I(12));
        Execute("x:=1;for(i:=1;i<5;i+=1){if(i==2)continue;x*=i;}");
        Scope.GetVar("x").Should().BeEquivalentTo(I(12));
        Execute("x:=1;for(i:range(1,5)){if(i==2)continue;x*=i;}");
        Scope.GetVar("x").Should().BeEquivalentTo(I(12));
    }

    [Fact]
    public void TestTagged()
    {
        Execute("x:=0;out:for(i:=0;i<5;i+=1)for(j:=0;j<5;j+=1){x+=i+j;if(i==3&&j==2)break out;}");
        Scope.GetVar("x").Should().BeEquivalentTo(I(57));
        Execute("x:=0;out:for(i:range(5))for(j:range(5)){x+=i+j;if(i==3&&j==2)break out;}");
        Scope.GetVar("x").Should().BeEquivalentTo(I(57));
        Execute("x:=0;out:for(i:=0;i<5;i+=1)for(j:=0;j<5;j+=1){x+=i+j;if(i==3&&j==2)continue out;}");
        Scope.GetVar("x").Should().BeEquivalentTo(I(87));
        Execute("x:=0;out:for(i:range(5))for(j:range(5)){x+=i+j;if(i==3&&j==2)continue out;}");
        Scope.GetVar("x").Should().BeEquivalentTo(I(87));
    }
}