using Irony.Parsing;

namespace Bish {

    internal class BishProgram {
        private readonly BishGrammar grammar;
        private Parser parser;
        public BishInterpreter bishInterpreter;

        public BishProgram() {
            grammar = new BishGrammar();
            parser = new Parser(grammar);
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
                    Console.WriteLine($"Result: {Parse(input)}");
                }
                catch (BishJumpException jump) {
                    if (jump.tag != null) throw;
                    else BishUtils.Error($"No loops found");
                }
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                if (Program.ShowErrorStack) Console.WriteLine($"Exception: {ex}");
                else Console.WriteLine($"Exception: {ex.Message}");
                Console.ResetColor();
            }
        }

        public static void PrintParseTree(ParseTreeNode node, int level = 0) {
            if (node == null) return;
            var isErrorNode = node.Term == null || node.Term.Name == "<error>";
            var name = node.Term == null ? "<error>" : node.Term.Name;
            if (node.ChildNodes.Count != 1) {
                Console.Write($"{new string('·', level * 2)}");
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write($"[{name}]");
                Console.ResetColor();
                Console.WriteLine($"{(node.ChildNodes.Count < 2 ? node.FindTokenAndGetText() : "")}"
                    + $"{(isErrorNode ? " (Error)" : "")}");
                foreach (var child in node.ChildNodes) {
                    PrintParseTree(child, level + 1);
                }
            }
            else PrintParseTree(node.ChildNodes[0], level);
        }
    }
}