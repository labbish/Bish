namespace BishTest.Lib;

public class FileTest(OptimizeInfoFixture fixture) : LibTest(fixture, "file", ["Reader", "Writer"])
{
    [Fact]
    public void TestFile()
    {
        Execute("with(writer:Writer('a.txt')) writer.write('abc\\n');");
        Execute("with(writer:Writer('a.txt',true)) writer.write('def');");
        ExpectResult("(){with(reader:Reader('a.txt', 'utf-8')) return reader.readChar();}()", S("a"));
        ExpectResult("(){with(reader:Reader('a.txt', 'utf-8')) return reader.readLine();}()", S("abc"));
        ExpectResult("(){with(reader:Reader('a.txt', 'utf-8')) return reader.chars.join();}()", S("abc\ndef"));
        ExpectResult("(){with(reader:Reader('a.txt', 'utf-8')) return reader.lines.join('\\n');}()", S("abc\ndef"));
        ExpectResult("(){with(reader:Reader('a.txt', 'utf-8')) return reader.content;}()", S("abc\ndef"));
    }
}