using BishBytecode;
using BishBytecode.Bytecodes;
using BishRuntime;

namespace Bish;

public class Repl(BishScope? scope = null)
{
    public readonly BishScope Scope = scope ?? BishScope.Globals;

    public void Loop()
    {
        Console.WriteLine("Welcome to Bish REPL!");
        Console.WriteLine("Type \".help\" for more information.");
        while (true)
        {
            Console.Write(">>> ");
            var code = Console.ReadLine();
            switch (code)
            {
                case null or "": break;
                case ".help":
                    Console.WriteLine(".clear  Clear screen.");
                    Console.WriteLine(".comp   Compile and print bytecode.");
                    Console.WriteLine(".exit   Exit the REPL.");
                    Console.WriteLine(".file   Load a file into the REPL session.");
                    Console.WriteLine(".help   Print this help message.");
                    break;
                case ".clear": Console.Clear(); break;
                case ".exit": return;
                case not null when code.StartsWith(".file"):
                    Handled(() =>
                    {
                        var file = code[5..].Trim().Trim('"');
                        var content = File.ReadAllText(file);
                        var frame = BishCompiler.BishCompiler.Compile(content, Scope);
                        frame.Execute();
                    });
                    break;
                case not null when code.StartsWith(".comp"):
                    Handled(() =>
                    {
                        var frame = BishCompiler.BishCompiler.Compile(code[5..], Scope);
                        foreach (var bytecode in frame.Bytecodes)
                            Console.WriteLine(BytecodeParser.ToString(bytecode));
                    });
                    break;
                default:
                    Handled(() =>
                    {
                        var frame = BishCompiler.BishCompiler.Compile(code, Scope);
                        frame.EndStatHandler = result => Handled(() =>
                        {
                            Scope.DefVar("_", result);
                            Console.WriteLine(result);
                        });
                        frame.Execute();
                    });
                    break;
            }
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