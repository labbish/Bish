using BishRuntime;

namespace Bish;

public static class Program
{
    public static async Task Main(string[] args)
    {
        BishCompiler.BishCompiler.Init();
        var options = Options.Parse(args);
        switch (options)
        {
            case { Server: true }:
                await LSP.Server.RunAsync();
                break;
            case { Source: null }:
                StartRepl();
                break;
            default:
                var frame = BishCompileService.Compile(options.Source);
                if (options.Output is { } output)
                {
                    await using var stream = File.Create(output);
                    stream.WriteBytecodes(frame);
                }

                if (options.SkipExecution) return;
                frame.Scope.DefVar("args", new BishList(options.Arguments
                    .Select(arg => new BishString(arg)).ToList<BishObject>()));
                try
                {
                    frame.Execute();
                }
                catch (BishException e)
                {
                    await Console.Error.WriteLineAsync($"Uncaught error: {e.Error}");
                    break;
                }
                catch (Exception e)
                {
                    await Console.Error.WriteLineAsync(e.ToString());
                    break;
                }

                if (options.Interactive) StartRepl(frame.Scope);
                break;
        }
    }

    public static void StartRepl(BishScope? scope = null) => BishImporter.Import(null, "repl").GetMember("start")
        .Call(new BishArgs([scope ?? BishNull.Instance as BishObject]));
}