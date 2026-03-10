namespace BishTest.Lib;

public class ThreadTest(OptimizeInfoFixture fixture) : LibTest(fixture, "thread", ["Thread", "Lock"])
{
    [Fact]
    public void TestThread()
    {
        Execute("s:=0;func f(){for(_:range(1000))s+=1;};");
        Execute("t1:=Thread(f);t2:=Thread(f);");
        Execute("t1.start();t2.start();");
        Execute("t1.join();t2.join();");
        Result("s").Should().BeOfType<BishInt>().Which.Value.Should().BeLessThan(2000);
        Result("Thread.id").Should().BeOfType<BishInt>();
    }
    
    [Fact]
    public void TestLock()
    {
        Execute("o:=object();s:=0;func f(){with(Lock(o))for(_:range(1000))s+=1;};");
        Execute("t1:=Thread(f);t2:=Thread(f);");
        Execute("t1.start();t2.start();");
        Execute("t1.join();t2.join();");
        ExpectResult("s", I(2000));
    }
}