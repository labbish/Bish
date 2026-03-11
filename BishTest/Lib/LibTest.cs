namespace BishTest.Lib;

public class LibTest : Test
{
    public LibTest(TestInfoFixture fixture, string module, string[] exports) : base(fixture) =>
        Execute($"{{{string.Join(',', exports.Select(s => '.' + s))}}}:=import('{module}');");
}