namespace Bish {

    internal class BishUtils {

        public static void Assert(bool condition, string message = "") {
            if (!condition) Error(message);
        }

        public static dynamic Error(string message = "") {
            throw new ArgumentException(message);
        }

        public static dynamic NotImplemented([CallerMemberName] string caller = "") {
            var stackTrace = new StackTrace();
            var methodInfo = stackTrace.GetFrame(1)?.GetMethod();
            var className = methodInfo?.DeclaringType?.Name;
            return Error($"Function '{className}.{caller}' Not Implemented");
        }

        public static void Todo(string todo) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"###TODO: {todo}");
            Console.ResetColor();
        }

        public static long GetID(object obj) {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}