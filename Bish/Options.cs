using System.Diagnostics.CodeAnalysis;
using BishRuntime;

namespace Bish;

internal class Options
{
    private const string HelpMsg =
        """
        * Bish Command Line Help *
        -c, --command: Runs the command string.
        -f, --file: Reads from the file.
        -o, --output: Compile and output to file.
        -s, --skip: Compile without executing.
        -i, --interactive: Enter REPL after executing.
        -l, --lsp: Run LSP only.
        -h, --help: Show this help message.
        """;

    public ICodeSource? Source;
    public string? Output;
    public bool SkipExecution;
    public bool Interactive;
    public bool Server;
    public bool Help;

    public static Options Parse(string[] arguments)
    {
        var args = new Stack<string>(arguments.Reverse());
        var options = new Options();
        while (args.TryPop(out var arg))
            switch (arg)
            {
                case "-c" or "--command":
                    if (!args.TryPop(out var command)) Error($"{arg} expect a command");
                    if (options.Source is not null) Error("found multiple input sources");
                    options.Source = new VirtualSource("<input>", command);
                    break;
                case "-f" or "--file":
                    if (!args.TryPop(out var file)) Error($"{arg} expect a file");
                    if (options.Source is not null) Error("found multiple input sources");
                    options.Source = new FileSource(file);
                    break;
                case "-o" or "--output":
                    if (!args.TryPop(out var output)) Error($"{arg} expect an output");
                    options.Output = output;
                    break;
                case "-s" or "--skip": options.SkipExecution = true; break;
                case "-i" or "--interactive": options.Interactive = true; break;
                case "-l" or "--lsp": options.Server = true; break;
                case "-h" or "--help": options.Help = true; break;
            }

        return options switch
        {
            { SkipExecution: true, Output: null } => Error("skip execution is invalid without output file"),
            { Source: null, Output: not null } => Error("output is invalid without input"),
            { Help: true } => ShowHelp(),
            _ => options
        };
    }

    [DoesNotReturn]
    public static Options Error(string msg)
    {
        Console.Error.WriteLine(msg);
        return ShowHelp();
    }

    [DoesNotReturn]
    public static Options ShowHelp()
    {
        Console.Error.WriteLine(HelpMsg);
        Environment.Exit(1);
        return null!;
    }
}