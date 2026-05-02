namespace BishTest.Core;

public class ReflectTest : Test
{
    public ReflectTest(TestInfoFixture fixture) : base(fixture)
    {
        Execute("class B{a:=3;};");
        Execute("class C:B{a:=0;init(self)self.b:=0;func f(self)self.a;};");
        Execute("class D{func f(self)-1;o:='d';};");
        Execute("c:=C();c.a:=c.b:=1;");
    }

    [Fact]
    public void TestObjectReflect()
    {
        ExpectResult("c.vars['a']", "1");
        ExpectResult("c.vars['b']", "1");
        Execute("c.vars['a']=2;del c.vars['b'];c.vars['c']:=3;");
        ExpectResult("c.a", "2");
        ExpectError("c.b;", BishError.AttributeErrorType);
        ExpectResult("c.c", "3");
        ExpectTrue("c.type===C");
        Execute("c.type=D;");
        ExpectResult("c.f()", "-1");
    }

    [Fact]
    public void TestScopeReflect()
    {
        Execute("s:=this().scope;f:=()'f';g:=()'g';h:=()'h';");
        ExpectResult("s.outer.outer", "null");
        Execute("s1:=null;{s1=this().scope;}");
        ExpectTrue("s1.outer===s");
        ExpectTrue("s.vars['f']===f");
        Execute("s.vars['f']=g;del s.vars['h'];");
        ExpectResult("f()", "'g'");
        ExpectError("h();", BishError.AttributeErrorType);
    }

    [Fact]
    public void TestPassCaller()
    {
        Execute("a:=1;func f(s)s.scope.a;f.passCaller=true;");
        ExpectResult("f()", "1");
        ExpectResult("{a:=2;f()}", "2");
        ExpectResult("f()", "1");
        Execute("a:=1;class F{oper()(self,s)s.scope.a;passCaller:=true};f:=F();");
        ExpectResult("f()", "1");
        ExpectResult("{a:=2;f()}", "2");
        ExpectResult("f()", "1");
    }

    [Fact]
    public void TestFrameReflect()
    {
        // Get "this" -> Call 0 -> Move "f"
        Execute("f:=this();");
        ExpectResult("ip:=f.ip", "3");
        Execute("x:=bytecode('Get',{.name:'this'});");
        Execute("y:=bytecode('Call',{.argc:0});");
        Execute("z:=bytecode('Move',{.name:'f'});");
        ExpectResult("f.bytecodes", "[x,y,z]");
        ExpectResult("f.scope.ip", "3");
        ExpectResult("f.outer", "null");
        ExpectError("bytecode('???',{});", BishError.BytecodeParserErrorType);

        Execute("a:=bytecode('Int',{.value:0});");
        Execute("b:=bytecode('Def',{.name:'k'});");
        Execute("f:=frame([a,b]);");
        ExpectResult("f.execute()", "null");
        ExpectResult("f.stack", "[0]");
        ExpectResult("f.scope.k", "0");

        ExpectResult("{a:=1;()0}.frame.scope.a", "1");
        ExpectResult("(()0).isGen", "false");
        ExpectResult("(()*0).isGen", "true");
        ExpectResult("(()0).isAsync", "false");
        ExpectResult("(()async 0).isAsync", "true");

        ExpectResult("meta.compile('return 0;').execute()", "0");
        ExpectError("meta.compile('???')", BishError.CompilationErrorType);
    }

    [Fact]
    public void TestParseTree()
    {
        const string c = "([BinOpExpr] ([AtomExpr] ([IntAtom] 1)) + ([AtomExpr] ([IntAtom] 2)))";
        Execute("t:=meta.parse('1+2');");
        ExpectResult("t.toString()", $"'([Program] {c} <EOF>)'");
        ExpectResult("t.type", "'Program'");
        ExpectResult("t.text", "'1+2<EOF>'");
        ExpectResult("t.parent", "null");
        ExpectResult("t.children.length", "2");

        Execute("c:=t.children[0];");
        ExpectResult("c.toString()", $"'{c}'");
        ExpectResult("c.type", "'BinOpExpr'");
        ExpectResult("c.text", "'1+2'");
        ExpectTrue("c.parent===t");
        ExpectResult("c.children.length", "3");

        ExpectResult("meta.compile(t).eval()", "3");
    }
}