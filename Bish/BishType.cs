using Irony.Parsing;

namespace Bish {

    internal class BishType {
        public string? type = null;
        public bool nullable = false;
        public bool isConst = false;

        public static readonly Dictionary<Type, string> TypeNames = [];

        static BishType() {
            TypeNames[typeof(int)] = "int";
            TypeNames[typeof(double)] = "num";
            TypeNames[typeof(string)] = "string";
            TypeNames[typeof(bool)] = "bool";
            TypeNames[typeof(BishInterval)] = "interval";
            TypeNames[typeof(BishFunc)] = "func";
        }

        public BishType(dynamic? value = null, string? type = null, bool? nullable = null, bool isConst = false) {
            this.type = type ?? GetTypeName(value);
            this.nullable = nullable ?? false;
            this.isConst = isConst;
        }

        public BishType(ParseTreeNode node) {
            (isConst, type, nullable) = CutType(node);
        }

        public BishType(string name) {
            this.type = name;
        }

        public BishType(dynamic? value, ParseTreeNode? node) {
            if (value is not null) type = GetTypeName(value);
            if (node is not null) (isConst, type, nullable) = CutType(node);
        }

        public static string? GetTypeName(dynamic? value) {
            if (value == null) return null;
            if (!TypeNames.ContainsKey(value.GetType())) return null;
            return value == null ? null : TypeNames[value.GetType()];
        }

        public static (bool, string, bool) CutType(ParseTreeNode node) {
            var parts = BishVars.ToPlainStrings(node);
            bool isConst = false;
            string type = "";
            bool nullable = false;
            if (parts.Last() == "?") {
                nullable = true;
                parts = parts[..^1];
            }
            if (parts.First() == "const") {
                isConst = true;
                parts = parts[1..];
            }
            BishUtils.Assert(parts.Count == 1, "Cannot Find Type Info");
            type = parts[0];
            return (isConst, type, nullable);
        }

        public override string ToString() {
            return $"{(isConst ? "const " : "")}{type ?? "(?)"}{(nullable ? "?" : "")}";
        }
    }
}