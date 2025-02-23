namespace Bish {

    internal class BishType(string name) : IBishExecutable {
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

        public static bool operator ==(BishType? a, BishType? b) {
            if (a is null) return false;
            if (b is null) return false;
            return a.name == b.name;
        } //TEMP

        public static bool operator !=(BishType? a, BishType? b) {
            return !(a == b);
        }

        public override string ToString() {
            return "[Type]";
        }

        public override bool Equals(object? obj) {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is null) return false;
            if (obj is BishType type) return type.name == name;
            return false;
        } //TEMP

        public override int GetHashCode() {
            return name.GetHashCode();
        }

        public BishVariable Exec(BishInArg[] args) {
            BishVariable var = new(null, type: this,
                value: new BishObject(this, (BishVars)members.Clone()));
            ((BishObject)var.value!).members.Exec(name, args);
            return var;
        }

        public bool MatchArgs(BishInArg[] args) {
            return members.GetMatchingFuncs(name, args).Count != 0;
        }
    }
}