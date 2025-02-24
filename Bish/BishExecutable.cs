namespace Bish {

    internal class BishArg
        (BishTypeInfo type, string name, BishVariable? defaultValue = null) {
        public BishTypeInfo type = type;
        public string name = name;
        public BishVariable? defaultValue = defaultValue;

        public BishArg(ParseTreeNode typeNode, string name, BishVariable? defaultValue = null)
            : this(type: new(typeNode), name, defaultValue) { }

        public override string ToString() {
            return $"{type} {name}";
        }
    }

    internal class BishInArg(string? name, BishVariable value) {
        public string? name = name;
        public BishVariable value = value;

        public BishInArg(BishVariable value) : this(null, value) {
        }

        public override string ToString() {
            return $"{name ?? ""}{(name is null ? "" : ":")}[{value}]";
        }
    }

    internal interface IBishExecutable {

        public BishVariable Exec(BishInArg[] args);

        public bool MatchArgs(BishInArg[] args);
    }
}