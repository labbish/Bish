namespace Bish {

    internal class BishArg {
        public BishTypeInfo type;
        public string name;
        public BishVariable? defaultValue;

        public BishArg(ParseTreeNode typeNode, string name, BishVariable? defaultValue = null)
            : this(type: new(typeNode), name, defaultValue) { }

        public BishArg(BishTypeInfo type, string name, BishVariable? defaultValue = null) {
            this.name = name;
            this.type = type;
            this.defaultValue = defaultValue;
        }

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