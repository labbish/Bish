namespace BishTest.Core;

public class AsyncTest(TestInfoFixture fixture) : Test(fixture)
{
    [Fact]
    public void TestRunner()
    {
        Execute("task:={.completed:false,.result:null,.s:0};");
        Execute("task.poll:=(ctx){task.s+=1;if(task.s>10){task.completed=true;task.result=0}else ctx.waker.awake();};");
        ExpectResult("Runner.blocked(task)", "0");
        ExpectResult("task.s", "11");
    }

    [Fact]
    public void TestTasks()
    {
        ExpectResult("Runner.blocked(Task.completed(0))", "0");
        ExpectErrorResult("Runner.blocked(Task.error(Error()))");
        ExpectResult("Runner.blocked(Task.run(()0))", "0");
        ExpectResult("Runner.blocked(Task.all(Task.completed(1),Task.completed(2)))", "[1,2]");
        ExpectResult("Runner.blocked(Task.any(Task.completed(1),Task.completed(2)))", "1");
        ExpectResult("Runner.blocked(Task.sleep(1000))", "null");
        ExpectResult("task:=Task.completed(0);task.cancel();Runner.blocked(task)", "null");
    }
}