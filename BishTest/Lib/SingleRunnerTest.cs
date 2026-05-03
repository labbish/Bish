namespace BishTest.Lib;

public class SingleRunnerTest(TestInfoFixture fixture)
    : LibTest(fixture, "single-thread-runner", ["SingleThreadRunner"])
{
    [Theory]
    [Repeat(5)]
    public void TestSingleRunner(int _)
    {
        Execute("func f()async{await Task.sleep(500);'f'};");
        Execute("func g()async{await Task.sleep(10);'g'};");
        Execute("func main()async await Task.merge(f(),g()).toList();");
        ExpectResult("SingleThreadRunner.blocked(main())", "['g','f']");
    }
}