namespace Bish {

    public class Program {
        public static readonly bool ShowParseTree = false;
        public static readonly bool ShowErrorStack = false;
        public static readonly bool DoUnitTests = false;
        public static readonly bool StopIfTestFailed = false;
        public static readonly bool StopIfTestFinished = true;

        private static BishProgram program = new();

        public static void Main() {
            BishUtils.Todo("bool operators");
            BishUtils.Todo("conditions");

            if (DoUnitTests) BishUnitTest.TestAll();

            while (true) {
                Console.Write(">>>");
                string input = Console.ReadLine()!;
                if (input == "end") break;
                if (input == "test") BishUnitTest.TestAll();
                program.Run(input);
            }
        }
    }
}