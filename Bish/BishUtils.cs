namespace Bish {

    internal class BishUtils {

        public static void Assert(bool condition, string message = "") {
            if (!condition) throw new ArgumentException(message);
        }
    }
}