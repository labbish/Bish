namespace BishTest.Lib;

public class SingleRunnerTest(TestInfoFixture fixture)
    : LibTest(fixture, "single-thread-runner", ["SingleThreadRunner"])
{
    [Fact]
    public void TestSingleRunner()
    {
        Execute("func f()async{await Task.sleep(1000);'f'};");
        Execute("func g()async{await Task.sleep(300);'g'};");
        Execute("func main()async await Task.merge(f(),g()).toList();");
        ExpectResult("SingleThreadRunner.blocked(main())", "['g','f']");
    }
}