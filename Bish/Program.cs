namespace Bish {

    public class Program {
        public static bool ShowParseTree = false;
        public static bool WholeParseTree = false;
        public static bool ShowErrorStack = false;
        public static bool ShowEvaluateTime = false;
        public static bool ShowEvaluateSteps = false;
        public static bool ShowVarsStackDepth = false;
        public static readonly bool DoUnitTests = false;
        public static readonly bool StopIfTestFailed = false;
        public static readonly bool StopIfTestFinished = false;

        private static BishProgram program = new();

        public static void Main(string[] args) {
            BishUtils.Todo("funcs (maybe?)");

            if (DoUnitTests) BishUnitTest.Test();

            if (args.Length == 1) ExecuteFile(args[0]);

            while (true) {
                Console.Write(">>>");
                string input = Console.ReadLine()!;
                string[] inputs = input.Split(' ');
                if (input == "end") break;
                else if (input == "test") BishUnitTest.Test();
                else if (input == "tree") ShowParseTree = !ShowParseTree;
                else if (input == "whole") WholeParseTree = !WholeParseTree;
                else if (input == "stack") ShowErrorStack = !ShowErrorStack;
                else if (input == "time") ShowEvaluateTime = !ShowEvaluateTime;
                else if (input == "steps") ShowEvaluateSteps = !ShowEvaluateSteps;
                else if (input == "depth") ShowVarsStackDepth = !ShowVarsStackDepth;
                else if (inputs.Length == 2 && inputs[0] == "test") {
                    if (int.TryParse(inputs[1], out int num)) BishUnitTest.Test(num);
                }
                else if (inputs.Length == 2 && inputs[0] == "open")
                    ExecuteFile(inputs[1]);
                else if (input == "vars")
                    Console.WriteLine($"vars = {program.bishInterpreter.vars}");
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