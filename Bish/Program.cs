using BishRuntime;
using CommandLine;

namespace Bish;

public static class Program
{
    public static async Task Main(string[] args) =>
        await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(RunOptionsAsync);

    private static async Task RunOptionsAsync(Options options)
    {
        BishCompiler.BishCompiler.Init();
        switch (options)
        {
            case { Server: true }:
                await Server.RunAsync();
                break;
            case { SkipExecution: true, Output: null }:
                throw new ArgumentException("-s is invalid without output file");
            case { Command : not null, File: not null }:
                throw new ArgumentException("-c and -f cannot work together");
            case { Command : null, File: null }:
                if (options.Output is not null) throw new ArgumentException("-o is invalid without input");
                new Repl().Loop();
                break;
            default:
                var frame = options.Command is null
                    ? BishCompileService.CompileFile(options.File!)
                    : BishCompileService.Compile(options.Command);
                if (options.Output is { } output)
                {
                    await using var stream = File.Create(output);
                    stream.WriteBytecodes(frame.Bytecodes);
                }

                if (options.SkipExecution) return;
                Repl.Handled(() => frame.Execute());
                if (options.Interactive) new Repl(frame.Scope).Loop();
                break;
        }
    }
}