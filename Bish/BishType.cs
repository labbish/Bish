namespace Bish {
    internal class BishType(string name) {
        public string name = name;
        public BishVars members = new();

        public override string ToString() {
            return name;
        }
    }
}
