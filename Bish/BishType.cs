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
            this._nullable = nullable ?? false;
            this.isConst = isConst;
            this.typeArgs = typeArgs ?? [];
        }

        public BishType(ParseTreeNode node) {
            (isConst, type, _nullable, typeArgs) = CutType(node);
        }

        public BishType(List<string> parts) {
            (isConst, type, _nullable, typeArgs) = CutType(parts);
        }

        public BishType(string name) {
            this.type = name;
        }

        public BishType(dynamic? value, ParseTreeNode? node) {
            if (node is not null) (isConst, type, _nullable, typeArgs) = CutType(node);
            if (value is not null) type = GetTypeName(value);
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
                    List<List<string>> argsList = Split(args, x => x == ",");
                    typeArgs = [.. argsList.Select(list => new BishType(list))];
                }
            }
            if (type is null) BishUtils.Error("Cannot find Type Info");
            return (isConst, type!, nullable, typeArgs);
        }

        public override string ToString() {
            return (isConst ? "const " : "")
                + (type ?? "(?)")
                + (typeArgs.Count > 0 ? "<" : "")
                + string.Join(',', typeArgs.Select(t => t is null ? "null" : t.ToString()))
                + (typeArgs.Count > 0 ? ">" : "")
                + (_nullable ? "?" : "");
        }

        private static List<List<T>> Split<T>(List<T> list, Predicate<T> predicate) {
            List<List<T>> ans = [];
            List<T> current = [];
            foreach (T t in list) {
                if (!predicate(t)) current.Add(t);
                else {
                    ans.Add([.. current]);
                    current = [];
                }
            }
            ans.Add([.. current]);
            return ans;
        }
    }
}