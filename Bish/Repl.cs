using BishBytecode;
using BishBytecode.Bytecodes;
using BishCompiler;
using BishRuntime;

namespace Bish;

public class Repl(BishScope? scope = null)
{
    public readonly BishScope Scope = scope ?? BishScope.Globals;

    public void Loop()
    {
        while (true)
        {
            Console.Write(">>> ");
            var code = Console.ReadLine();
            if (string.IsNullOrEmpty(code)) continue;
            if (code == ".exit") break;
            if (code.StartsWith(".file"))
                Handled(() =>
                {
                    var file = code[5..].Trim().Trim('"');
                    var content = File.ReadAllText(file);
                    var frame = BishCompiler.BishCompiler.Compile(content, Scope);
                    frame.Execute();
                });
            else
                Handled(() =>
                {
                    var frame = BishCompiler.BishCompiler.Compile(code, Scope, ErrorHandlingMode.Ignore);
                    if (frame.Bytecodes[^1] is Pop) frame.Bytecodes.RemoveAt(frame.Bytecodes.Count - 1);
                    frame.Execute();
                    if (frame.Stack.Count == 0) return;
                    var result = frame.Stack.Pop();
                    Scope.DefVar("_", result);
                    Console.WriteLine(result);
                });
        }
    }

    public static void Handled(Action action)
    {
        try
        {
            action();
        }
        catch (BishException e)
        {
            Console.Error.WriteLine(e.Error);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }
}