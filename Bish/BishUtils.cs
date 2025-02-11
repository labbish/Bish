namespace Bish {

    internal class BishUtils {

        public static void Assert(bool condition, string message = "") {
            if (!condition) Error(message);
        }

        public static dynamic Error(string message = "") {
            throw new ArgumentException(message);
        }
    }
}