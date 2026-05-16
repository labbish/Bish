namespace BishTest.Core;

public class AsyncTest(TestInfoFixture fixture) : Test(fixture)
{
    [Theory]
    [Repeat(5)]
    public void TestRunner(int _)
    {
        Execute("task:={.completed:false,.result:null,.s:0};");
        Execute("task.poll:=(ctx){task.s+=1;if(task.s>10){task.result=0;task.completed=true}else ctx.waker.awake();};");
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
        Execute("x:=1;for await(i:Task.merge(Task.run(()1),Task.run(()2),Task.run(()3),Task.run(()4)))x*=i;");
        ExpectResult("x", "24");
        Execute("l:=[];for await(i:Task.concat(Task.run(()1),Task.run(()2),Task.run(()3),Task.run(()4)))l.add(i);");
        ExpectResult("l", "[1,2,3,4]");
        ExpectResult("await Task.run(()1).map((x)x*2)", "2");
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
        ExpectResult("s:={.g:()async await f()+2};await s.g()", "2");
        ExpectResult("await 0", "0");
        ExpectError("func h()async throw Error('error');await h();", BishError.StaticType, "error");
    }

    [Theory]
    [Repeat(5)]
    public void TestForAwait(int _)
    {
        Execute("class I:Iterator{new(){.r:range(1,5)};func next(self)async self.r.next()};");
        Execute("x:=1;for await(i:I())x*=i;");
        ExpectResult("x", "24");
    }

    [Theory]
    [Repeat(5)]
    public void TestWithAwait(int _)
    {
        Execute("a:=0;class W{func dispose(self)async a+=1;};");
        Execute("with await(W()){};");
        Execute("try with await(_:W())throw Error('error');");
        ExpectResult("a", "2");
    }

    [Theory]
    [Repeat(5)]
    public void TestAsyncGen(int _)
    {
        Execute("func f()async*{for(i:range(1,5)){await i;yield await i;}};");
        Execute("x:=1;for await(i:f())x*=i;");
        ExpectResult("x", "24");
        Execute("func g()async*{yield 2;yield await* f()};");
        Execute("x:=1;for await(i:g())x*=i;");
        ExpectResult("x", "48");
    }
}