using System;
using Bish;
using Irony.Parsing;

public class Program {

    public static void Main(string[] args) {
        var grammar = new BishGrammar();
        var parser = new Parser(grammar);
        var bishInterpreter = new BishInterpreter();
        while (true) {
            Console.Write(">>>");
            string input = Console.ReadLine();
            if (input == "end") break;
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
    }

    public static void PrintParseTree(ParseTreeNode node, int level = 0) {
        var isErrorNode = node.Term.Name == "<error>";
        Console.WriteLine(new string(' ', level * 2) + node.Term.Name + (isErrorNode ? " (Error)" : ""));

        foreach (var child in node.ChildNodes) {
            PrintParseTree(child, level + 1);
        }
    }
}