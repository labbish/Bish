﻿namespace Bish {

    public class Program {
        public static readonly bool ShowParseTree = false;
        public static readonly bool ShowErrorStack = false;
        public static readonly bool DoUnitTests = false;
        public static readonly bool StopIfTestFailed = false;
        public static readonly bool StopIfTestFinished = false;

        private static BishProgram program = new();

        public static void Main(string[] args) {
            BishUtils.Todo("execute from files");
            BishUtils.Todo("shortcut: parse (e.g. true||3.14 should fail)");
            BishUtils.Todo("conditions");

            if (DoUnitTests) BishUnitTest.TestAll();

            if (args.Length == 1) ExecuteFile(args[0]);

            while (true) {
                Console.Write(">>>");
                string input = Console.ReadLine()!;
                string[] inputs = input.Split(' ');
                if (input == "end") break;
                else if (input == "test") BishUnitTest.TestAll();
                else if (inputs.Length == 2 && inputs[0] == "open")
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