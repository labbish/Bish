using CommandLine;
using JetBrains.Annotations;

namespace Bish;

internal class Options
{
    [Option('c', "command", Required = false, HelpText = "Runs the command string.", SetName = "input")]
    public string? Command { get; [UsedImplicitly] set; }
    
    [Option('f', "file", Required = false, HelpText = "Reads from the file.", SetName = "input")]
    public string? File { get; [UsedImplicitly] set; }

    [Option('o', "output", Required = false, HelpText = "Compile and output to file.")]
    public string? Output { get; [UsedImplicitly] set; }

    [Option('s', "skip", Required = false, HelpText = "Compile without executing.")]
    public bool SkipExecution { get; [UsedImplicitly] set; }

    [Option('i', "interactive", Required = false, HelpText = "Enter REPL after executing.")]
    public bool Interactive { get; [UsedImplicitly] set; }
}