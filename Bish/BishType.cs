using Irony.Parsing;

namespace Bish {

    internal class BishType {
        public string? type = null;
        public bool _nullable = false;
        public bool isConst = false;
        public List<BishVariable> typeArgs = [];

        public bool nullable {
            get { return IsNullable(); }
            set { _nullable = value; }
        }

        private bool IsNullable() {
            if (type == "var" && typeArgs.Count != 0) {
                BishUtils.Assert(typeArgs.All(arg => arg.value is BishType));
                return typeArgs.Any(T => T.value!.IsNullable());
            }
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
            bool isConst = false, List<BishVariable>? typeArgs = null) {
            this.type = type ?? GetTypeName(value);
            _nullable = nullable ?? false;
            this.isConst = isConst;
            this.typeArgs = typeArgs ?? [];
            Simplify();
        }

        public BishType(string name) {
            type = name;
            Simplify();
        }

        public static string? GetTypeName(dynamic? value) {
            if (value is null) return null;
            if (!TypeNames.ContainsKey(value.GetType())) return null;
            return value is null ? null : TypeNames[value.GetType()];
        }

        public static BishType operator |(BishType a, BishType b) {
            return new BishType(type: "var", typeArgs:
                [new(null, value: a), new(null, value: b)]).Simplify();
        }

        public static BishVariable operator ==(BishType a, BishType b) {
            a.Simplify();
            b.Simplify();
            return BishUtils.NotImplemented();
            return new(null, value: a.type == b.type && a.typeArgs == b.typeArgs);
        }

        public static BishVariable operator !=(BishType a, BishType b) {
            return BishUtils.NotImplemented();
        }

        private BishType Simplify() {
            if (type == "var" && typeArgs.Count > 0) {
                BishUtils.Assert(typeArgs.All(arg => arg.value is BishType));
                List<BishType> types = PossibleTypes(this);
                var typeGroups = types.GroupBy(t => t.type).ToList();
                if (typeGroups.Any(group => group.Key == "var")) {
                    if (types.Any(type => type.nullable))
                        return new(type: "var", nullable: true);
                    return new(type: "var", nullable: false);
                }
                typeArgs = [..typeGroups.Select(group => new BishType(type: group.Key,
                    nullable: group.Any(type => type.nullable)))
                    .Select(type => new BishVariable(null, value: type))];
                if (nullable)
                    foreach (BishType? type in typeArgs.Select(arg => arg.value as BishType))
                        type!._nullable = true;
            }
            return this;
        }

        private static List<BishType> PossibleTypes(BishType T) {
            if (T.type == "var" && T.typeArgs.Count > 0) {
                BishUtils.Assert(T.typeArgs.All(arg => arg.value is BishType));
                List<BishType> ans = [];
                foreach (BishType? sub in T.typeArgs.Select(arg => arg.value as BishType))
                    ans.AddRange(PossibleTypes(sub!));
                return ans;
            }
            return [T];
        }

        public override string ToString() {
            return (isConst ? "const " : "")
                + (type ?? "(?)")
                + (typeArgs.Count > 0 ? "<" : "")
                + string.Join(',', typeArgs.Select(t => t is null ? "null" : t.ValueString()))
                + (typeArgs.Count > 0 ? ">" : "")
                + (_nullable ? "?" : "");
        }

        public override bool Equals(object? obj) {
            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj is null) {
                return false;
            }

            return false;
        }

        public override int GetHashCode() {
            return HashCode.Combine(type, _nullable, isConst, typeArgs);
        }
    }
}