using Irony.Parsing;

namespace Bish {

    internal class BishProgram {
        private readonly BishGrammar grammar;
        private Parser parser;
        private BishInterpreter bishInterpreter;

        public BishProgram() {
            grammar = new BishGrammar();
            parser = new Parser(grammar);
            bishInterpreter = new BishInterpreter();
        }

        public BishVariable Parse(string input) {
            var parseTree = parser.Parse(input);
            if (Program.ShowParseTree) PrintParseTree(parseTree.Root);
            if (parseTree.HasErrors())
                BishUtils.Error($"Parse Error: {parseTree.ParserMessages[0].Message}");
            return bishInterpreter.Interpret(parseTree);
        }

        public void Run(string input) {
            try {
                Console.WriteLine($"Result: {Parse(input)}");
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
            Console.WriteLine($"{new string(' ', level * 2)}[{name}]"
                + $"{node.FindTokenAndGetText()}{(isErrorNode ? " (Error)" : "")}");

            foreach (var child in node.ChildNodes) {
                PrintParseTree(child, level + 1);
            }
        }
    }
}