using Irony.Parsing;

namespace Bish {

    internal class BishProgram {
        private BishGrammar grammar;
        private Parser parser;
        private BishInterpreter bishInterpreter;

        public BishProgram() {
            grammar = new BishGrammar();
            parser = new Parser(grammar);
            bishInterpreter = new BishInterpreter();
        }

        public BishVariable Parse(string input) {
            var parseTree = parser.Parse(input);
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
                Console.WriteLine($"Exception: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}