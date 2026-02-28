using BishBytecode.Bytecodes;
using CommandLine;
using String = BishBytecode.Bytecodes.String;

namespace Bish;

public static class Program
{
    public static void Main(string[] args) => Parser.Default.ParseArguments<Options>(args).WithParsed(RunOptions);

    private static void RunOptions(Options options)
    {
        switch (options)
        {
            case { SkipExecution: true, Output: null }:
                throw new ArgumentException("-s is invalid without output file");
            case { Command : not null, File: not null }: throw new ArgumentException("-c and -f cannot work together");
            case { Command : null, File: null }:
                if (options.Output is not null) throw new ArgumentException("-o is invalid without input");
                new Repl().Loop();
                break;
            default:
                var input = options.Command ?? File.ReadAllText(options.File!);
                var root = Repl.FindRoot(options.File);
                var frame = BishCompiler.BishCompiler.Compile(input, out _, root: root);
                if (options.Output is {} output)
                    File.WriteAllText(output, string.Join("\n", frame.Bytecodes.Select(BytecodeParser.ToString)));
                if (options.SkipExecution) return;
                frame.Execute();
                if (options.Interactive) new Repl(frame.Scope).Loop();
                break;
        }
    }
}