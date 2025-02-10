using Bish;
using Irony.Parsing;

public class Program {
    private static BishGrammar grammar = new BishGrammar();
    private static Parser parser = new Parser(grammar);
    private static BishInterpreter bishInterpreter = new BishInterpreter();

    public static void Main(string[] args) {
        while (true) {
            Console.Write(">>>");
            string input = Console.ReadLine()!;
            if (input == "end") break;
            if (input.EndsWith(';')) input = input.Remove(input.Length - 1);
            string[] sentences = input.Split(';');
            foreach (string sentence in sentences) Parse(sentence);
        }
    }

    private static void Parse(string input) {
        var parseTree = parser.Parse(input);
        if (parseTree.HasErrors()) {
            foreach (var error in parseTree.ParserMessages) {
                Console.WriteLine(error.Message);
            }
        }
        else {
            //PrintParseTree(parseTree.Root);
            try {
                bishInterpreter.Interpret(parseTree);
            }
            catch (Exception ex) {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
    }

    public static void PrintParseTree(ParseTreeNode node, int level = 0) {
        var isErrorNode = node.Term.Name == "<error>";
        Console.WriteLine(new string(' ', level * 2) + node.Term.Name
            + (isErrorNode ? " (Error)" : ""));

        foreach (var child in node.ChildNodes) {
            PrintParseTree(child, level + 1);
        }
    }
}