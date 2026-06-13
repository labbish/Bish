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
                    Console.WriteLine(".opts   Output optimization info.");
                    Console.WriteLine(".help   Print this help message.");
                    break;
                case ".clear": Console.Clear(); break;
                case ".exit": return;
                case ".opts": Console.WriteLine(BishOptimizer.Info()); break;
                case not null when code.StartsWith(".file"):
                    Handled(() =>
                    {
                        var file = code[5..].Trim().Trim('"');
                        var frame = BishCompileService.Compile(new FileSource(file), scope: Scope);
                        frame.Execute();
                    });
                    break;
                case not null when code.StartsWith(".comp"):
                    Handled(() =>
                    {
                        var frame = BishCompileService.Compile(new VirtualSource("<input>", 
                            code[5..].Trim()), scope: Scope);
                        foreach (var bytecode in frame.Bytecodes)
                            Console.WriteLine((bytecode.Pos?.ShortString() ?? "??")
                                              + "\t" + BishBytecodeParser.ToString(bytecode));
                    });
                    break;
                default:
                    Handled(() =>
                    {
                        var frame = BishCompileService.Compile(new VirtualSource("<input>", code), scope: Scope);
                        frame.Execute();
                        if (!frame.Stack.TryPeek(out var result)) return;
                        Scope.DefMember("_", result);
                        Console.WriteLine(BishString.CallDebug(result));
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
            Console.Error.WriteLine($"Uncaught error: {e.Error}");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }
}