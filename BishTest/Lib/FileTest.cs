namespace BishTest.Lib;

public class FileTest : LibTest
{
    public FileTest(TestInfoFixture fixture) : base(fixture, "file", ["Reader", "Writer"])
    {
        TryRemove("./f");
        CreateDirectory("./f");
    }

    [Fact]
    public void TestFile()
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