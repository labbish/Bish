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
                await Server.RunAsync();
                break;
            case { Source: null }:
                new Repl().Loop();
                break;
            default:
                var frame = BishCompileService.Compile(options.Source);
                if (options.Output is { } output)
                {
                    await using var stream = File.Create(output);
                    stream.WriteBytecodes(frame);
                }

                if (options.SkipExecution) return;
                Repl.Handled(() => frame.Execute());
                if (options.Interactive) new Repl(frame.Scope).Loop();
                break;
        }
    }
}