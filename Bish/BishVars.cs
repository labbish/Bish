﻿namespace Bish {

    internal class BishVars : IEnumerable<BishVariable>, ICloneable {
        public HashSet<BishVariable> vars;

        private void AddFrom(HashSet<BishVariable> other) {
            vars = [.. vars.Concat(other)];
        }

        private void InitBuiltIns() {
            AddFrom(BishBuiltInFuncs.GetBuiltIns());
        }

        public BishVars(bool builtIn = true) {
            vars = [];
            if (builtIn) InitBuiltIns();
        }

        private BishVars(HashSet<BishVariable> vars) : this(builtIn: false) {
            AddFrom(vars);
        }

        public void Clear() {
            vars = [];
            InitBuiltIns();
        }

        public BishVars(BishVars original) {
            vars = [.. original.vars];
        }

        public object Clone() {
            BishVars ans = new(this);
            Dictionary<BishVariable, BishVariable> mapping
                = ans.vars.ToDictionary(var => var, var => (BishVariable)var.Clone());
            ans.Map(mapping);
            return ans;
        }

        public static BishVars? GetVars(BishVariable var) {
            return var.value switch {
                BishObject obj => obj.members,
                BishType type => type.members,
                BishTypeInfo type => type.type?.members,
                BishFunc func => func.varsFrame,
                _ => null,
            };
        }

        private void Map(Dictionary<BishVariable, BishVariable> mapping,
            List<BishVariable>? origins = null) {
            origins ??= [];
            BishVars ans = new([.. vars.Select(var =>
            mapping.TryGetValue(var, out BishVariable? value) ? value : var)]);
            foreach (BishVariable var in ans) {
                if (origins.Any(mapping.ContainsValue)
                    && mapping.ContainsValue(var)) continue;
                BishVars? vars = GetVars(var);
                if (vars is null) continue;
                vars.Map(mapping, [.. origins.Concat([var])]);
            }
            vars = ans.vars;
        } //Problems with func self-containing

        public IEnumerator<BishVariable> GetEnumerator() {
            return vars.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return vars.GetEnumerator();
        }

        private static void VarLog(string msg, BishVariable var) {
            if (Program.ShowVarUsing) Console.WriteLine($"{msg}: {var.name}"
                + (Program.ShowVarObjectID ? $" [ID={BishUtils.GetID(var)}]" : ""));
        }

        public BishVariable Get(ParseTreeNode node, bool checkNull = true,
            List<BishVariable>? except = null) {
            string name = node.FindTokenAndGetText();
            return Get(name, checkNull, except);
        }

        public BishVariable Get(string name, bool checkNull = true,
            List<BishVariable>? except = null) {
            except ??= [];
            var matched = vars.Where(var => var.name == name && !except.Contains(var)).ToHashSet();
            var values = matched.Select(var => checkNull ? var.GetNullChecked() : var).ToHashSet();
            BishUtils.Assert(values.Count <= 1, $"Multiple variables found: {name}");
            BishUtils.Assert(values.Count > 0, $"Variable not found: {name}");
            VarLog("Get", values.First());
            return values.First();
        }

        public HashSet<BishVariable> GetMatchingFuncs(string name, BishInArg[] args,
            List<IBishExecutable>? except = null) {
            return [..vars
                .Where(var => var.name == name)
                .Where(var => var.value is IBishExecutable && var.value is not null)
                .Where(var => !(except??[]).Contains(var.value))
                .Where(var => var.value!.MatchArgs(args))];
        }

        public BishVariable Exec(ParseTreeNode node, BishInArg[] args,
            List<IBishExecutable>? except = null) {
            string name = node.FindTokenAndGetText();
            return Exec(name, args, except);
        }

        public BishVariable Exec(string name, BishInArg[] args,
            List<IBishExecutable>? except = null) {
            except ??= [];
            var funcs = GetMatchingFuncs(name, args, except);
            BishUtils.Assert(funcs.Count <= 1, $"Multiple Functions found: {name}");
            foreach (BishVariable func in funcs) {
                VarLog("Exec", func);
                return func.Exec(args);
            }
            return BishUtils.Error($"Function not found: {name}");
        }

        public BishVariable Set(ParseTreeNode node, BishVariable value,
            bool checkConst = true, bool checkExist = true) {
            string name = node.FindTokenAndGetText();
            return Set(name, value, checkConst, checkExist);
        }

        public BishVariable Set(string name, BishVariable value,
            bool checkConst = true, bool checkExist = true) {
            if (name.All(c => c == '_')) return new(null, value.value);
            var matched = vars.Where(var => var.name == name).ToHashSet();
            foreach (BishVariable var in matched) {
                VarLog("Set", var);
                if (checkConst)
                    BishUtils.Assert(!var.type.isConst, $"Cannot modify const var: {name}");
                BishVariable newVar = new(null, value.value);
                WeakConvert(var.type, newVar); //might throw
                var.value = newVar.value;
                return new(null, value.value);
            }
            if (checkExist) return BishUtils.Error($"Variable not found: {name}");
            return new(null);
        }

        public BishVariable New(ParseTreeNode node, BishVariable value, bool checkExist = true) {
            string name = node.FindTokenAndGetText();
            return New(name, value, checkExist);
        }

        public BishVariable New(string name, BishVariable value, bool checkExist = true) {
            if (name.All(c => c == '_')) return new(null, value.value);
            if (checkExist) {
                var matched = vars.Where(var => var.name == name).ToHashSet();
                BishUtils.Assert(matched.Count == 0, $"Var {name} already exists");
            }
            vars.Add(new BishVariable(name: name, type: value.type, value: value.value));
            return value;
        }

        public static BishVariable WeakConvert(BishTypeInfo type, BishVariable var) {
            return WeakConvert(type, var, out _);
        }

        public static BishVariable WeakConvert(BishTypeInfo type, BishVariable var,
            out int ConvertTimes) {
            bool converted = false;
            dynamic? value = null;
            ConvertTimes = 0;
            if (type.nullable && var.value is null) {
                converted = true;
            }
            if (!type.nullable && var.value is null) {
                BishUtils.Error("Cannot convert null value to not nullable type");
            }
            if (type.type == "var" && type.typeArgs.Count > 0) {
                BishUtils.Assert(type.typeArgs.All(arg => arg.value is BishTypeInfo));
                foreach (BishTypeInfo? subType in type.typeArgs.Select(arg => arg.value as BishTypeInfo)) {
                    try {
                        BishVariable result = WeakConvert(subType!, var, out int subConvertTimes);
                        ConvertTimes = subConvertTimes;
                        result.type = type;
                        return result;
                    }
                    catch (Exception) { }
                }
            }
            if (type.type == "var" && type.typeArgs.Count == 0 && var.value is not null) {
                value = var.value;
                converted = true;
                ConvertTimes += 1;
            }
            if (type.type == "num" && type.typeArgs.Count == 0) {
                if (var.value is BishNum num) {
                    value = num;
                    converted = true;
                }
                else if (var.value is BishInt i) {
                    value = (BishNum)i;
                    converted = true;
                    ConvertTimes++;
                }
            }
            else if (type.type == "int" && type.typeArgs.Count == 0) {
                if (var.value is BishInt num) {
                    value = num;
                    converted = true;
                }
            }
            else if (type.type == "string" && type.typeArgs.Count == 0) {
                if (var.value is string str) {
                    value = str;
                    converted = true;
                }
            }
            else if (type.type == "bool" && type.typeArgs.Count == 0) {
                if (var.value is bool b) {
                    value = b;
                    converted = true;
                }
            }
            else if (type.type == "interval" && type.typeArgs.Count == 0) {
                if (var.value is BishInterval i) {
                    value = i;
                    converted = true;
                }
            }
            else if (type.type == "type" && type.typeArgs.Count == 0) {
                if (var.value is BishTypeInfo t) {
                    value = t;
                    converted = true;
                }
            }
            else if (type.type == "func" && type.typeArgs.Count <= 1) {
                if (var.value is IBishExecutable f) {
                    value = f;
                    if (type.typeArgs.Count == 1) {
                        BishUtils.Assert(type.typeArgs.All(arg => arg.value is BishTypeInfo));
                        value.returnType = type.typeArgs[0].value;
                    }
                    converted = true;
                }
            }
            else if (type.type == var.type.type) {
                value = var.value;
                converted = true;
            }
            BishUtils.Assert(converted, $"Cannot convert [{var}] into type {type}");
            return new BishVariable(name: null, type: type, value);
        }

        public override string ToString() {
            return "{\n  "
                + string.Join("\n  ",
                vars.Where(var => !var.builtIn || Program.ShowBuiltInObject)
                .Select(var => $"{var.name}: {var.ValueString()}"
                + (Program.ShowVarObjectID ? $" [ID={BishUtils.GetID(var)}]" : "")))
                + "\n}";
        }

        public static List<string> ToPlainStrings(ParseTreeNode node) {
            if (node.ChildNodes.Count == 0) return [node.FindTokenAndGetText()];
            List<string> strings = [];
            foreach (ParseTreeNode child in node.ChildNodes) {
                strings.AddRange(ToPlainStrings(child));
            }
            return strings;
        }

        public static string ToPlainString(ParseTreeNode node) {
            return string.Join("·", ToPlainStrings(node));
        }
    }
}