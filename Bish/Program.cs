using Bish;
using Irony.Parsing;

public class Program {
    private static BishProgram program = new();

    public static void Main() {
        BishUnitTest.TestAll();

        while (true) {
            Console.Write(">>>");
            string input = Console.ReadLine()!;
            if (input == "end") break;
            if (input.EndsWith(';')) input = input.Remove(input.Length - 1);
            string[] sentences = input.Split(';');
            foreach (string sentence in sentences) program.Run(sentence);
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