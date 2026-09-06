using BishRuntime;

namespace Bish;

public static class Program
{
    public static void Main(string[] args)
    {
        BishCompiler.BishCompiler.Init();
        var main = BishImporter.Import(null, "$self/main").GetMember("main");
        var lsp = new BishNativeTask(LSP.Server.RunAsync);
        var argv = args.Select(arg => new BishString(arg)).ToArray();
        BishTaskRunner.Block(main.Call(new BishArgs([lsp, ..argv])));
    }
}