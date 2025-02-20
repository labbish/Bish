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
                return _nullable || typeArgs.Any(T => T.value!.IsNullable());
            }
            return _nullable;
        }

        public static readonly Dictionary<Type, string> TypeNames = [];

        static BishType() {
            TypeNames[typeof(BishInt)] = "int";
            TypeNames[typeof(BishNum)] = "num";
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

        public static string? GetTypeName(dynamic? value) {
            if (value is null) return null;
            if (!TypeNames.ContainsKey(value.GetType())) return null;
            return value is null ? null : TypeNames[value.GetType()];
        }

        public static BishType operator |(BishType a, BishType b) {
            return new BishType(type: "var", typeArgs:
                [new(null, value: a), new(null, value: b)]).Simplify();
        }

        public static bool operator ==(BishType a, BishType b) {
            a.Simplify();
            b.Simplify();
            if (a.typeArgs.Count != b.typeArgs.Count) return false;
            if (!(a.type == b.type && a.nullable == b.nullable && a.isConst == b.isConst))
                return false;
            if (a.type == "var") {
                BishUtils.Assert(a.typeArgs.All(arg => arg.value is BishType));
                BishUtils.Assert(b.typeArgs.All(arg => arg.value is BishType));
                List<BishVariable> args1 = [.. a.typeArgs];
                List<BishVariable> args2 = [.. b.typeArgs];
                while (args1.Count != 0) {
                    BishVariable arg1 = args1[0];
                    List<BishVariable> found = [.. args2.Where(arg => (arg == arg1).value)];
                    if (found.Count != 1) return false;
                    args1.Remove(arg1);
                    args2.Remove(found[0]);
                }
                return true;
            }
            int count = a.typeArgs.Count;
            for (int i = 0; i < count; i++)
                if ((a.typeArgs[i] != b.typeArgs[i]).value) return false;
            return true;
        }

        public static bool operator !=(BishType a, BishType b) {
            return !(a == b);
        }

        public BishType Simplify() {
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
            if (type == "var" && typeArgs.Count == 1) {
                BishType sub = (typeArgs[0].value as BishType)!;
                nullable = nullable || sub.nullable;
                type = sub.type;
                typeArgs = [];
            }
            return this;
        }

        private static List<BishType> PossibleTypes(BishType T) {
            if (T.type == "var" && T.typeArgs.Count > 0) {
                BishUtils.Assert(T.typeArgs.All(arg => arg.value is BishType));
                List<BishType> ans = [];
                foreach (BishType? sub in T.typeArgs.Select(arg => arg.value as BishType)) {
                    sub!.nullable = sub!.nullable || T.nullable;
                    ans.AddRange(PossibleTypes(sub!));
                }
                return ans;
            }
            return [T];
        }

        public override string ToString() {
            return (isConst ? "const " : "")
                + (type ?? "(?)")
                + (typeArgs.Count > 0 ? "[" : "")
                + string.Join(',', typeArgs.Select(t => t is null ? "null" : t.ValueString()))
                + (typeArgs.Count > 0 ? "]" : "")
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