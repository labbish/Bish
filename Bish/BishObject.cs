namespace Bish {

    internal class BishObject(BishType type, BishVars members) : ICloneable {
        public BishType type = type;
        public BishVars members = members;

        public object Clone() {
            return new BishObject(type, (BishVars)members.Clone());
        }
    }
}