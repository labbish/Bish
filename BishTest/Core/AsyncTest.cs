namespace BishTest.Core;

public class AsyncTest(TestInfoFixture fixture) : Test(fixture)
{
    [Theory]
    [Repeat(5)]
    public void TestRunner(int _)
    {
        Execute("task:={.completed:false,.result:null,.s:0};");
        Execute("task.poll:=(ctx){task.s+=1;if(task.s>10){task.completed=true;task.result=0}else ctx.waker.awake();};");
        ExpectResult("Runner.blocked(task)", "0");
        ExpectResult("task.s", "11");
    }

    [Theory]
    [Repeat(5)]
    public void TestTasks(int _)
    {
        ExpectResult("Runner.blocked(Task.completed(0))", "0");
        ExpectError("Runner.blocked(Task.error(Error('error')))", BishError.StaticType, "error");
        ExpectResult("Runner.blocked(Task.run(()0))", "0");
        ExpectResult("Runner.blocked(Task.all(Task.completed(1),Task.completed(2)))", "[1,2]");
        ExpectResult("Runner.blocked(Task.any(Task.completed(1),Task.completed(2)))", "1");
        ExpectResult("Runner.blocked(Task.sleep(10))", "null");
        ExpectResult("task:=Task.completed(0);task.cancel();Runner.blocked(task)", "null");
    }

    [Theory]
    [Repeat(5)]
    public void TestAsync(int _)
    {
        Execute("func f()async 0;func g()async{await f()+1};");
        ExpectResult("Runner.blocked(f())", "0");
        ExpectResult("Runner.blocked(g())", "1");
        ExpectResult("x:=await f()", "0");
        ExpectResult("x", "0");
        ExpectResult("s:={.g};await s.g()", "1");
        ExpectResult("await 0", "0");
        ExpectError("func h()async throw Error('error');await h();", BishError.StaticType, "error");
    }
}