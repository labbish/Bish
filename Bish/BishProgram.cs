
namespace Bish {

    internal class BishProgram {
        private static readonly BishGrammar grammar;
        private static Parser parser;
        public BishInterpreter bishInterpreter;

        static BishProgram() {
            Stopwatch watch = new();
            watch.Start();
            grammar = new();
            parser = new(grammar);
            watch.Stop();
            Console.WriteLine($"Parser Initialized after {watch.Elapsed.TotalMilliseconds:N2}ms");
        }

        public BishProgram() {
            bishInterpreter = new BishInterpreter();
        }

        public BishVariable Parse(string input) {
            var parseTree = parser.Parse(input);
            if (Program.ShowParseTree) PrintParseTree(parseTree.Root);
            if (parseTree.HasErrors())
                foreach (var msg in parseTree.ParserMessages) {
                    BishUtils.Error($"Parse Error: {msg.Message}, "
                        + $"at Line {msg.Location.Line}, Column {msg.Location.Column}");
                }
            return bishInterpreter.Interpret(parseTree);
        }

        public void Run(string input) {
            try {
                try {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Result: {Parse(input)}");
                }
                catch (BishJumpException jump) {
                    if (jump.tag is not null) throw;
                    else BishUtils.Error($"No loops found");
                }
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                if (Program.ShowErrorStack) Console.WriteLine($"Exception: {ex}");
                else Console.WriteLine($"Exception: {ex.Message}");
            }
            finally {
                Console.ResetColor();
            }
        }

        public static void PrintParseTree(ParseTreeNode node, int level = 0) {
            if (node is null) return;
            var isErrorNode = node.Term is null || node.Term.Name == "<error>";
            var name = node.Term is null ? "<error>" : node.Term.Name;
            if (node.ChildNodes.Count != 1 || Program.WholeParseTree) {
                Console.Write($"{new string('·', level * 2)}");
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write("[");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(name);
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write("]");
                Console.ResetColor();
                if (node.ChildNodes.Count < 2)
                    Console.WriteLine($"{node.FindTokenAndGetText()}{(isErrorNode ? " (Error)" : "")}");
                else {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"(×{node.ChildNodes.Count})");
                    Console.ResetColor();
                }
                foreach (var child in node.ChildNodes) {
                    PrintParseTree(child, level + 1);
                }
            }
            else PrintParseTree(node.ChildNodes[0], level);
        }
    }
}