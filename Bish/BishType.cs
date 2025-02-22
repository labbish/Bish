namespace Bish {
    internal class BishType(string name) {
        public string name = name;
        public BishVars members = new();

        public static implicit operator BishType(string name) {
            return new(name);
        }

        public static implicit operator string?(BishType? type) {
            return type?.name;
        }

        public static implicit operator BishTypeInfo(BishType type) {
            return new(type: type);
        }

        public override string ToString() {
            return "[Type]";
        }
    }
}
