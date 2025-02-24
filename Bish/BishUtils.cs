namespace Bish {

    internal class BishUtils {

        public static void Assert([DoesNotReturnIf(false)] bool condition,
            string message = "", [CallerMemberName] string caller = "") {
            var className = GetCallerClassName(new StackTrace());
            if (!condition) Error($"{message} at '{className}.{caller}'", showCaller: false);
        }

        [DoesNotReturn]
        public static dynamic Error(string message,
            [CallerMemberName] string caller = "", bool showCaller = true) {
            if (!showCaller) throw new ArgumentException(message);
            var className = GetCallerClassName(new StackTrace());
            throw new ArgumentException($"{message} at '{className}.{caller}'");
        }

        [DoesNotReturn]
        public static dynamic Impossible([CallerMemberName] string caller = "") {
            var className = GetCallerClassName(new StackTrace());
            return Error($"Impossible Case in '{className}.{caller}'");
        }

        [DoesNotReturn]
        public static dynamic NotImplemented([CallerMemberName] string caller = "") {
            var className = GetCallerClassName(new StackTrace());
            return Error($"Function '{className}.{caller}' Not Implemented");
        }

        private static string? GetCallerClassName(StackTrace stackTrace) {
            return stackTrace.GetFrame(1)?.GetMethod()?.DeclaringType?.Name;
        }

        public static void Todo(string todo) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"###TODO: {todo}");
            Console.ResetColor();
        }

        public static int GetID(object obj) {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}