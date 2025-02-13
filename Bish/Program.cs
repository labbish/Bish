namespace Bish {

    public class Program {
        public static readonly bool ShowParseTree = true;
        public static readonly bool ShowErrorStack = false;
        public static readonly bool DoUnitTests = false;
        public static readonly bool StopIfTestFailed = false;
        public static readonly bool StopIfTestFinished = false;

        private static BishProgram program = new();

        public static void Main(string[] args) {
            BishUtils.Todo("split between blocks & sentence (e.g. '{...}...' without ';' cannot be parsed)");
            BishUtils.Todo("if/for should only get nearest sentence without '{' and '}'");
            BishUtils.Todo("jump out of loops");

            if (DoUnitTests) BishUnitTest.Test();

            if (args.Length == 1) ExecuteFile(args[0]);

            while (true) {
                Console.Write(">>>");
                string input = Console.ReadLine()!;
                string[] inputs = input.Split(' ');
                if (input == "end") break;
                else if (input == "test") BishUnitTest.Test();
                else if (inputs.Length == 2 && inputs[0] == "test") {
                    if (int.TryParse(inputs[1], out int num)) BishUnitTest.Test(num);
                }
                else if (inputs.Length == 2 && inputs[0] == "open"
                    )
                    ExecuteFile(inputs[1]);
                else program.Run(input);
            }
        }

        private static void ExecuteFile(string filename) {
            try {
                filename = Trim(filename);
                string content = File.ReadAllText(filename);
                program.Run(content);
            }
            catch (Exception ex) {
                Console.WriteLine($"Cannot Read File {filename}: {ex.Message}");
            }
        }

        private static string Trim(string filename) {
            if (filename.First() == '"' && filename.Last() == '"')
                return filename[1..^1];
            return filename;
        }
    }
}