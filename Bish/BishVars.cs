﻿using Irony.Parsing;
using System.Collections;

namespace Bish {

    internal class BishVars : IEnumerable<BishVariable> {
        public HashSet<BishVariable> vars;

        public BishVars() {
            vars = [];
        }

        public BishVars(BishVars original) {
            vars = [.. original.vars];
        }

        public IEnumerator<BishVariable> GetEnumerator() {
            return vars.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return vars.GetEnumerator();
        }

        public BishVariable Get(ParseTreeNode node) {
            string name = node.FindTokenAndGetText();
            var matched = vars.Where(var => var.name == name).ToHashSet();
            foreach (BishVariable var in matched) return var.GetNullChecked();
            return BishUtils.Error($"Variable not found: {name}");
        }

        public BishVariable GetUnchecked(ParseTreeNode node) {
            string name = node.FindTokenAndGetText();
            var matched = vars.Where(var => var.name == name).ToHashSet();
            foreach (BishVariable var in matched) return var;
            return BishUtils.Error($"Variable not found: {name}");
        }

        public BishVariable Set(ParseTreeNode node, BishVariable value) {
            string name = node.FindTokenAndGetText();
            var matched = vars.Where(var => var.name == name).ToHashSet();
            foreach (BishVariable var in matched) {
                BishUtils.Assert(!var.isConst, $"Cannot modify const var: {name}");
                BishVariable newVar = new(null, value.value);
                WeakConvert(var.type, newVar, var.nullable); //might throw
                var.value = newVar.value;
                return new BishVariable(null, value.value);
            }
            return BishUtils.Error($"Variable not found: {name}");
        }

        public BishVariable New(ParseTreeNode node, BishVariable value) {
            string name = node.FindTokenAndGetText();
            var matched = vars.Where(var => var.name == name).ToHashSet();
            BishUtils.Assert(matched.Count == 0, $"Var {name} already exists");
            vars.Add(new BishVariable(name, value.value, value.type, value.nullable, value.isConst));
            return value;
        }

        public static BishVariable WeakConvert(string? type, BishVariable var, bool nullable = false) {
            bool converted = false;
            dynamic? value = null;
            if (nullable && var.value == null) {
                converted = true;
            }
            if (type == "num") {
                if (var.value is double num) {
                    value = num;
                    converted = true;
                }
                else if (var.value is int i) {
                    value = i;
                    converted = true;
                }
            }
            else if (type == "int") {
                if (var.value is int num) {
                    value = num;
                    converted = true;
                }
            }
            else if (type == "string") {
                if (var.value is string str) {
                    value = str;
                    converted = true;
                }
            }
            else if (type == "bool") {
                if (var.value is bool b) {
                    value = b;
                    converted = true;
                }
            }
            BishUtils.Assert(converted, $"Cannot convert [{var}] into type {type}");
            return new BishVariable(null, value, type, nullable);
        }

        private static List<string> PlainListFromTree(ParseTreeNode node) {
            if (node.ChildNodes.Count == 0) return [node.FindTokenAndGetText()];
            List<string> strings = [];
            foreach (ParseTreeNode child in node.ChildNodes) {
                strings.AddRange(PlainListFromTree(child));
            }
            return strings;
        }

        public static (bool, string, bool) CutType(ParseTreeNode node) {
            var parts = PlainListFromTree(node);
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
    }
}