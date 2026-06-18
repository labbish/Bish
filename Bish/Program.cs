using System.Diagnostics.CodeAnalysis;
using BishRuntime;

namespace Bish;

public static class Server
{
    [Builtin]
    [SuppressMessage("Usage", "VSTHRD002")]
    public static void Run() => LSP.Server.RunAsync().GetAwaiter().GetResult();

    public static readonly BishType StaticType = new("Server");
}

public static class Program
{
    public static void Main(string[] args)
    {
        BishCompiler.BishCompiler.Init();
        BuiltinsRegistry.Register();
        var argv = args.Select(arg => new BishString(arg)).ToArray();
        BishImporter.Import(null, "$self/main").GetMember("main").Call(new BishArgs([Server.StaticType, ..argv]));
    }
}