global using Codes = System.Collections.Generic.List<BishRuntime.BishBytecode>;
global using Antlr4.Runtime.Tree;
global using Antlr4.Runtime;
global using BishRuntime;
using System.Reflection;
using BishSdk;

namespace BishCompiler;

public static class BishCompiler
{
    public static BishFrame Compile(string code, out List<CompilationError> errors,
        BishScope? scope = null, string? root = null, bool optimize = true, bool runs = true)
    {
        var stream = CharStreams.fromString(code);
        var lexer = new BishLexer(stream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new BishParser(tokens);

        var listener = new ErrorListener();
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);

        var program = parser.program();
        var visitor = new BishVisitor();
        var result = visitor.VisitFull(program, optimize: optimize);

        if (runs && result.Errors.Count > 0)
            throw new Exception("Crucial compile error(s) occured: " + string.Join("\n", result.Errors));
        errors = [..listener.Errors, ..result.Errors];

        var frame = new BishFrame(result.Codes, scope);
        frame.Scope.DefVar("import", new BishFunc("import", [new BishArg("file", BishString.StaticType)], args =>
        {
            var path = "(unresolved)";
            var file = args[0].ExpectToBe<BishString>("file").Value;
            if (BishScope.BuiltinModules.TryGetValue(file, out var mod)) return mod;
            try
            {
                path = Path.GetFullPath(root is null ? file : Path.Combine(root, file));
                if (ImportCache.TryGetValue(path, out var cached)) return cached;
                var module = new BishObject();
                if (path.EndsWith(".dll"))
                {
                    var assembly = Assembly.LoadFrom(path);
                    var types = assembly.GetTypes().Where(type =>
                        type is { IsClass: true, IsAbstract: false, IsPublic: true } &&
                        typeof(IPlugin).IsAssignableFrom(type)).ToList();
                    if (types.Count == 0)
                        throw new ArgumentException($"Cannot find plugin initializer in {path}: " +
                                                    $"found types {string.Join(", ", assembly.GetTypes())}, none of which implements IPlugin");
                    foreach (var type in types)
                    {
                        var plugin = Activator.CreateInstance(type) as IPlugin;
                        var exports = new PluginExports();
                        plugin?.Initialize(exports);
                        foreach (var (name, value) in exports.Exports)
                            module.DefMember(name, value);
                    }
                }
                else
                {
                    var imported = Compile(File.ReadAllText(path), out var errors);
                    if (errors.Count != 0) throw new ArgumentException(errors[0].Message);
                    imported.Execute();
                    foreach (var (name, value) in imported.Scope.Vars)
                        module.DefMember(name, value);
                }

                ImportCache.Add(path, module);
                return module;
            }
            catch (BishException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw BishException.OfImport(path, e.ToString());
            }
        }));
        return frame;
    }

    public static readonly Dictionary<string, BishObject> ImportCache = [];

    static BishCompiler() => BishLib.BishLib.Initialize();
}

public record CompilationError(
    int Line,
    int Column,
    string Message,
    int StopLine,
    int StopColumn,
    string? StackTrace = null)
{
    public override string ToString() =>
        $"Compilation error at line {Line}, column {Column} to line {StopLine}, column {StopColumn}: {Message}"
        + (StackTrace is null ? "" : "\n" + StackTrace);
}

public class ErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    public List<CompilationError> Errors { get; } = [];

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        var length = offendingSymbol.StopIndex - offendingSymbol.StartIndex + 1;
        var stopColumn = charPositionInLine + length;
        Errors.Add(new CompilationError(line, charPositionInLine, msg, line, stopColumn));
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        Errors.Add(new CompilationError(line, charPositionInLine, msg, line, charPositionInLine + 1));
    }
}