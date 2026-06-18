namespace BishTest.Lib;

public class FileTest : LibTest
{
    public FileTest(TestInfoFixture fixture) : base(fixture, "file", ["Path", "Reader", "Writer"])
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
        ExpectTrue("Path.sep is of string");
        // TODO: check if these exists
        ExpectTrue("Path.cwd is of Path");
        ExpectTrue("Path.home is of Path");
        ExpectTrue("Path.temp is of Path");
    }

    [Fact]
    public void TestWriterReader()
    {
        Execute("with(writer:Writer('./f/a.txt')) await writer.write('abc\\n');");
        Execute("with(writer:Writer('./f/a.txt',true)) await writer.write('def');");
        ExpectResult("with(reader:Reader('./f/a.txt','utf-8')) await reader.readChar()", "'a'");
        ExpectResult("with(reader:Reader('./f/a.txt','utf-8')) await reader.readLine()", "'abc'");
        ExpectResult("with(reader:Reader('./f/a.txt','utf-8')) await reader.readAll()", "'abc\ndef'");
        ExpectResult("with(reader:Reader('./f/a.txt','utf-8')) await reader.chars.join()", "'abc\ndef'");
        ExpectResult("with(reader:Reader('./f/a.txt','utf-8')) await reader.lines.join('\\n')", "'abc\ndef'");
    }
}