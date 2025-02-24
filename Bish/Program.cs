global using Irony.Parsing;
global using System.Reflection;
global using System.Diagnostics;
global using System.Collections;
global using System.Runtime.CompilerServices;

namespace Bish {

    public class Program {
        public const int MaxThreadCount = 128;
        public static bool ShowVarUsing = false;
        public static bool ShowParseTree = false;
        public static bool WholeParseTree = false;
        public static bool ShowErrorStack = false;
        public static bool ShowVarObjectID = false;
        public static bool ShowEvaluateTime = false;
        public static bool ShowBuiltInObject = false;
        public static bool ShowEvaluateSteps = false;
        public static bool ShowVarsStackDepth = false;
        public static readonly bool DoUnitTests = false;
        public static readonly bool StopIfTestFailed = false;
        public static readonly bool StopIfTestFinished = false;

        private static BishProgram program = new();

        public static void Main(string[] args) {
            BishUtils.Todo("classes");

            if (DoUnitTests) BishUnitTest.Test();

            if (args.Length == 1) ExecuteFile(args[0]);

            while (true) {
                Console.Write(">>>");
                string input = Console.ReadLine()!;
                string[] inputs = input.Split(' ');
                if (input == "end") break;
                else if (input == "test") BishUnitTest.Test();
                else if (input == "use") ShowVarUsing = !ShowVarUsing;
                else if (input == "tree") ShowParseTree = !ShowParseTree;
                else if (input == "id") ShowVarObjectID = !ShowVarObjectID;
                else if (input == "whole") WholeParseTree = !WholeParseTree;
                else if (input == "stack") ShowErrorStack = !ShowErrorStack;
                else if (input == "time") ShowEvaluateTime = !ShowEvaluateTime;
                else if (input == "steps") ShowEvaluateSteps = !ShowEvaluateSteps;
                else if (input == "depth") ShowVarsStackDepth = !ShowVarsStackDepth;
                else if (input == "builtin") ShowBuiltInObject = !ShowBuiltInObject;
                else if (input == "clear") program.bishInterpreter.vars.Clear();
                else if (inputs.Length == 2 && inputs[0] == "test") {
                    if (int.TryParse(inputs[1], out int num)) BishUnitTest.Test(num);
                }
                else if (inputs.Length == 2 && inputs[0] == "open")
                    ExecuteFile(inputs[1]);
                else if (input == "vars")
                    Console.WriteLine($"vars = {program.bishInterpreter.vars}");
                else if (inputs.Length == 2 && inputs[0] == "vars") {
                    try {
                        Console.WriteLine($"vars = {BishVars.GetVars(program.Parse(inputs[1]))}");
                    }
                    catch (Exception) { }
                }
                else program.Run(input + "\n");
            }
        }

        private static void ExecuteFile(string filename) {
            try {
                filename = Trim(filename);
                string content = File.ReadAllText(filename);
                program.Run(content + "\n");
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