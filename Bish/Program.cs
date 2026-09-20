// #define SIMPLE_DEBUG_REPL

using BishRuntime;

namespace Bish;

public static class Program
{
    public static void Main(string[] args)
    {
#if !SIMPLE_DEBUG_REPL
        var main = BishImporter.Import(null, "$self/main").GetMember("main");
        var lsp = new BishNativeTask(LSP.Server.RunAsync);
        var argv = args.Select(arg => new BishString(arg)).ToArray();
        BishTaskRunner.Block(main.Call(new BishArgs([lsp, ..argv])));
#else
        var scope = BishScope.Globals;
        while (true)
        {
            Console.Write(">>> ");
            var expr = Console.ReadLine();
            if (expr is null) return;
            var frame = BishMeta.Compile(null!, new BishCodeSource(new VirtualSource("<code>", "bish", expr)), scope);
            if (frame.Eval() is { } result) Console.WriteLine(BishString.CallDebug(result));
        }
#endif
    }

    static Program()
    {
        BishBuiltinBinder.Init();
        BishLib.BishLib.Initialize();
#if !SIMPLE_DEBUG_REPL
        try
        {
            BishImporter.Import(null, "preludes");
        }
        catch (BishException e)
        {
            Console.Error.WriteLine($"Cannot import preludes: {e.Error}");
        }
#endif
    }
}