namespace BishTest.Lib;

public class LibTest : CompilerTest
{
    public LibTest(OptimizeInfoFixture fixture, string module, string[] exports) : base(fixture) =>
        Execute($"{{{string.Join(',', exports.Select(s => '.' + s))}}}:=import('{module}');");
}