using Bish;
using Irony.Parsing;

public class Program {
    public static readonly bool ShowErrorStack = false;
    public static readonly bool DoUnitTests = false;
    public static readonly bool StopIfTestFailed = false;
    public static readonly bool StopIfTestFinished = false;

    private static BishProgram program = new();

    public static void Main() {
        BishUtils.Todo("complete nullable");

        if (DoUnitTests) BishUnitTest.TestAll();

        while (true) {
            Console.Write(">>>");
            string input = Console.ReadLine()!;
            if (input == "end") break;
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