namespace Bish {

    internal class BishJumpException(string pos, string? tag = null)
        : Exception($"Cannot find tag: {tag ?? "null"}") {
        public readonly string? tag = tag;
        public readonly Position pos = GetPosition(pos);

        public enum Position {
            START,
            END,
            NEXT,

            [Obsolete("Jump Pos 'Last' is not Implemented")]
            LAST,
        };

        public static Position GetPosition(string pos) {
            return Enum.Parse<Position>(pos.ToUpper());
        }
    }
}