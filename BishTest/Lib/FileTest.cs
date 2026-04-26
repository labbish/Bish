namespace BishTest.Lib;

public class FileTest(TestInfoFixture fixture) : LibTest(fixture, "file", ["Reader", "Writer"])
{
    [Fact]
    public void TestFile()
    {
        Execute("with(writer:Writer('a.txt')) writer.write('abc\\n');");
        Execute("with(writer:Writer('a.txt',true)) writer.write('def');");
        ExpectResult("((){with(reader:Reader('a.txt', 'utf-8')) reader.readChar()})()", "'a'");
        ExpectResult("((){with(reader:Reader('a.txt', 'utf-8')) reader.readLine()})()", "'abc'");
        ExpectResult("((){with(reader:Reader('a.txt', 'utf-8')) reader.chars.join()})()", "'abc\ndef'");
        ExpectResult("((){with(reader:Reader('a.txt', 'utf-8')) reader.lines.join('\\n')})()", "'abc\ndef'");
        ExpectResult("((){with(reader:Reader('a.txt', 'utf-8')) reader.content})()", "'abc\ndef'");
    }
}