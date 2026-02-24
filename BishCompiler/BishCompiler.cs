using Antlr4.Runtime;
using BishBytecode;

namespace BishCompiler;

public static class BishCompiler
{
    public static BishFrame Compile(string code, BishScope? scope = null)
    {
        var stream = CharStreams.fromString(code);
        var lexer = new BishLexer(stream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new BishParser(tokens);
        var tree = parser.program();
        var visitor = new BishVisitor();
        var codes = visitor.Visit(tree);
        return new BishFrame(codes, scope);
    }
}