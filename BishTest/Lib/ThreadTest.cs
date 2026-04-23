namespace BishTest.Lib;

public class ThreadTest(TestInfoFixture fixture) : LibTest(fixture, "thread", ["Thread", "Lock"])
{
    [Fact]
    public void TestThread()
    {
        Execute("s:=0;func f(){for(_:range(10000))s+=1;};");
        Execute("t1:=Thread(f);t2:=Thread(f);");
        Execute("t1.start();t2.start();");
        Execute("t1.join();t2.join();");
        ExpectTrue("s is of int and <20000");
        ExpectTrue("Thread.id is of int");
    }
    
    [Fact]
    public void TestLock()
    {
        Execute("o:=object();s:=0;func f(){with(Lock(o))for(_:range(10000))s+=1;};");
        Execute("t1:=Thread(f);t2:=Thread(f);");
        Execute("t1.start();t2.start();");
        Execute("t1.join();t2.join();");
        ExpectResult("s", "20000");
    }
}