namespace Bish {

    internal class BishJumpException(string pos, string? tag = null)
        : Exception($"Cannot find tag: {tag ?? "null"}") {
        public readonly string? tag = tag;
        public readonly Position pos = GetPosition(pos);

        public enum Position {
            START,
            END,
            LAST,
            NEXT,
        };

        public static Position GetPosition(string pos) {
            return Enum.Parse<Position>(pos.ToUpper());
        }
    }
}