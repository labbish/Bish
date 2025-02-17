using Irony.Parsing;

namespace Bish {

    internal class BishType {
        public string? type = null;
        public bool _nullable = false;
        public bool isConst = false;
        public List<BishType> typeArgs = [];

        public bool nullable {
            get { return IsNullable(); }
            set { _nullable = value; }
        }

        private bool IsNullable() {
            if (type == "var" && typeArgs.Count != 0)
                return typeArgs.Any(T => T.IsNullable());
            return _nullable;
        }

        public static readonly Dictionary<Type, string> TypeNames = [];

        static BishType() {
            TypeNames[typeof(int)] = "int";
            TypeNames[typeof(double)] = "num";
            TypeNames[typeof(string)] = "string";
            TypeNames[typeof(bool)] = "bool";
            TypeNames[typeof(BishInterval)] = "interval";
            TypeNames[typeof(BishFunc)] = "func";
            TypeNames[typeof(BishType)] = "type";
        }

        public BishType(dynamic? value = null, string? type = null, bool? nullable = null,
            bool isConst = false, List<BishType>? typeArgs = null) {
            this.type = type ?? GetTypeName(value);
            _nullable = nullable ?? false;
            this.isConst = isConst;
            this.typeArgs = typeArgs ?? [];
            Simplify();
        }

        public BishType(ParseTreeNode node) {
            (isConst, type, _nullable, typeArgs) = CutType(node);
            Simplify();
        }

        public BishType(List<string> parts) {
            (isConst, type, _nullable, typeArgs) = CutType(parts);
            Simplify();
        }

        public BishType(string name) {
            this.type = name;
            Simplify();
        }

        public BishType(dynamic? value, ParseTreeNode? node) {
            if (node is not null) (isConst, type, _nullable, typeArgs) = CutType(node);
            if (value is not null) type = GetTypeName(value);
            Simplify();
        }

        public static string? GetTypeName(dynamic? value) {
            if (value == null) return null;
            if (!TypeNames.ContainsKey(value.GetType())) return null;
            return value == null ? null : TypeNames[value.GetType()];
        }

        public static (bool, string, bool, List<BishType>) CutType(ParseTreeNode node) {
            var parts = BishVars.ToPlainStrings(node);
            return CutType(parts);
        }

        public static (bool, string, bool, List<BishType>) CutType(List<string> parts) {
            bool isConst = false;
            string? type = null;
            bool nullable = false;
            List<BishType> typeArgs = [];
            if (parts.Last() == "?") {
                nullable = true;
                parts = parts[..^1];
            }
            if (parts.First() == "const") {
                isConst = true;
                parts = parts[1..];
            }
            if (parts.Count == 1) type = parts[0];
            else if (parts.Count >= 4) {
                if (parts[1] == "<" && parts.Last() == ">") {
                    type = parts[0];
                    List<string> args = parts[2..^1];
                    List<List<string>> argsList = [];
                    List<string> current = [];
                    int depth = 0;
                    foreach (string arg in args) {
                        if (depth == 0 && arg == ",") {
                            argsList.Add(current);
                            current = [];
                            continue;
                        }
                        current.Add(arg);
                        if (arg == "<") depth++;
                        if (arg == ">") depth--;
                    }
                    argsList.Add(current);
                    typeArgs = [.. argsList.Select(list => new BishType(list))];
                }
            }
            if (type is null) BishUtils.Error("Cannot find Type Info");
            return (isConst, type!, nullable, typeArgs);
        }

        public static BishType operator |(BishType a, BishType b) {
            return new BishType(type: "var", typeArgs: [a, b]).Simplify();
        }

        public static BishVariable operator ==(BishType a, BishType b) {
            a.Simplify();
            b.Simplify();
            return new(null, value: a.type == b.type && a.typeArgs == b.typeArgs
                && BishUtils.NotImplemented());
        }

        public static BishVariable operator !=(BishType a, BishType b) {
            return BishUtils.NotImplemented();
        }

        private BishType Simplify() {
            if (type == "var" && typeArgs.Count > 0) {
                List<BishType> types = PossibleTypes(this);
                var typeGroups = types.GroupBy(t => t.type).ToList();
                if (typeGroups.Any(group => group.Key == "var")) {
                    if (types.Any(type => type.nullable))
                        return new(type: "var", nullable: true);
                    return new(type: "var", nullable: false);
                }
                typeArgs = [..typeGroups.Select(group => new BishType(type: group.Key,
                    nullable: group.Any(type => type.nullable)))];
                if (nullable) foreach (BishType type in typeArgs) type._nullable = true;
            }
            return this;
        }

        private static List<BishType> PossibleTypes(BishType T) {
            if (T.type == "var" && T.typeArgs.Count > 0) {
                List<BishType> ans = [];
                foreach (BishType sub in T.typeArgs)
                    ans.AddRange(PossibleTypes(sub));
                return ans;
            }
            return [T];
        }

        public override string ToString() {
            return (isConst ? "const " : "")
                + (type ?? "(?)")
                + (typeArgs.Count > 0 ? "<" : "")
                + string.Join(',', typeArgs.Select(t => t is null ? "null" : t.ToString()))
                + (typeArgs.Count > 0 ? ">" : "")
                + (_nullable ? "?" : "");
        }
    }
}