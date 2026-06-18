namespace BishTest.Lib;

public class FileTest : LibTest
{
    public FileTest(TestInfoFixture fixture) : base(fixture, "file", ["Path"])
    {
        TryRemove("./f");
        CreateDirectory("./f");
    }

    [Fact]
    public void TestPath()
    {
        Execute("p:=Path('/a/b/c.d');extend Path{get reg(self)self.value.replace(Path.sep,'/')};");
        ExpectResult("p.value", "'/a/b/c.d'");
        ExpectResult("p.name", "'c.d'");
        ExpectResult("p.stem", "'c'");
        ExpectResult("p.ext", "'.d'");
        ExpectResult("p.dir.reg", "'/a/b'");
        ExpectResult("p.root.reg", "'/'");
        ExpectResult("p.withExt('e').reg", "'/a/b/c.e'");
        ExpectResult("(p/'e').reg", "'/a/b/c.d/e'");
        ExpectTrue("Path('../a/b').full.reg.endsWith('/a/b')");
        ExpectResult("p.relative(Path('/a/c')).reg", "'../b/c.d'");
        ExpectFalse("p.isRelative");
        ExpectTrue("Path('../a/b').isRelative");
        ExpectTrue(@"Path.sep is '/' or '\\'");
        ExpectTrue("Path.cwd.existsDir");
        ExpectTrue("Path.home.existsDir");
        ExpectTrue("Path.temp.existsDir");
    }

    [Fact]
    public void TestFile()
    {
        Execute("p:=Path('./f/a.txt');");
        ExpectFalse("p.exists");
        ExpectTrue("p.create();p.exists");
        Execute("q:=Path('./f/b.txt');");
        ExpectTrue("p.copyTo(q);p.exists&&q.exists");
        ExpectFalse("p.delete();p.exists");
        ExpectTrue("q.moveTo(p);p.exists&&!q.exists");

        Execute("with(writer:p.write()) await writer.write('abc\\n');");
        Execute("with(writer:p.write(true)) await writer.write('def');");
        ExpectResult("with(reader:p.read()) await reader.readChar()", "'a'");
        ExpectResult("with(reader:p.read()) await reader.readLine()", "'abc'");
        ExpectResult("with(reader:p.read()) await reader.readAll()", "'abc\ndef'");
        ExpectResult("with(reader:p.read()) await reader.chars.join()", "'abc\ndef'");
        ExpectResult("with(reader:p.read()) await reader.lines.join('\\n')", "'abc\ndef'");
    }

    [Fact]
    public void TestDirectory()
    {
        Execute("p:=Path('./f/x');");
        ExpectFalse("p.existsDir");
        ExpectTrue("p.createDir();p.existsDir");
        Execute("q:=Path('./f/y');");
        ExpectTrue("p.copyDirTo(q);p.existsDir&&q.existsDir");
        ExpectFalse("p.deleteDir();p.existsDir");
        ExpectTrue("q.moveDirTo(p);p.existsDir&&!q.existsDir");

        ExpectResult("p.children.toList()", "[]");
        Execute("(p/'a.txt').create();");
        Execute("(p/'b.txt').create();");
        Execute("(p/'c').createDir();");
        ExpectResult("p.children.map((p)p.stem).toList().sorted()", "['a','b','c']");
    }
}