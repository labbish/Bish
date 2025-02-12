﻿using Irony.Parsing;
using System.Diagnostics;

namespace Bish {

    internal class BishInterpreter {
        public BishVars vars;

        public BishInterpreter() {
            vars = new BishVars();
        }

        public List<BishVariable> Interpret(ParseTree parseTree) {
            if (parseTree.Root == null) return BishUtils.Error("Parse tree is empty.");
            return Evaluate(parseTree.Root);
        }

        private BishVariable EvaluateSingle(ParseTreeNode node) {
            //Console.WriteLine(node.Term.Name);
            if (node == null) return new BishVariable(null);
            else if (node.Term is IdentifierTerminal) {
                return vars.Get(node);
            }
            else if (node.Term is NumberLiteral) {
                var str = node.Token.Value.ToString();
                BishUtils.Assert(str != null, "NumberLiteral is Null");
                dynamic value;
                if (str!.Contains('.')) value = double.Parse(str!);
                else value = int.Parse(str!);
                return new BishVariable(null, value);
            }
            else if (node.Term is StringLiteral) {
                var str = node.Token.Value.ToString();
                BishUtils.Assert(str != null, "StringLiteral is Null");
                return new BishVariable(null, str!);
            }
            else if (node.Term.Name == "boolLiteral") {
                var str = node.FindTokenAndGetText();
                BishUtils.Assert(str != null, "BoolLiteral is Null");
                bool b = str switch {
                    "true" => true,
                    "false" => false,
                    _ => BishUtils.Error($"{str} is not bool"),
                };
                return new BishVariable(null, b);
            }
            else if (node.Term.Name == "null") {
                return new BishVariable(null);
            }
            else if (node.Term is NonTerminal) {
                if (node.ChildNodes.Count == 0) return new BishVariable(null);
                if (node.ChildNodes.Count == 1) return EvaluateSingle(node.ChildNodes[0]);
                if (node.ChildNodes.Count == 2 && node.Term.Name == "factor") {
                    var sign = node.ChildNodes[0].FindTokenAndGetText();
                    var value = EvaluateSingle(node.ChildNodes[1]);
                    if (sign == "+") return +value;
                    if (sign == "-") return -value;
                }
                if (node.ChildNodes.Count == 2 && node.Term.Name == "statement") {
                    string type = node.ChildNodes[0].FindTokenAndGetText();
                    bool nullable = node.ChildNodes[0].ChildNodes.Count == 2
                        && node.ChildNodes[0].ChildNodes[1].FindTokenAndGetText() == "?";
                    BishVariable var = new(null, null, type, nullable);
                    return vars.New(node.ChildNodes[1], var);
                }
                if (node.ChildNodes.Count == 3 && node.ChildNodes[0].FindTokenAndGetText() == "(" && node.ChildNodes[2].FindTokenAndGetText() == ")")
                    return EvaluateSingle(node.ChildNodes[1]);
                if (node.ChildNodes.Count == 3 && node.ChildNodes[1].FindTokenAndGetText() == "=") {
                    var name = node.ChildNodes[0];
                    BishVariable right = EvaluateSingle(node.ChildNodes[2]);
                    return vars.Set(name, right);
                }
                if (node.ChildNodes.Count == 3) {
                    BishVariable left = EvaluateSingle(node.ChildNodes[0]);
                    BishVariable right = EvaluateSingle(node.ChildNodes[2]);
                    string op = node.ChildNodes[1].FindTokenAndGetText();
                    return op switch {
                        "+" => left + right,
                        "-" => left - right,
                        "*" => left * right,
                        "/" => left / right,
                        "^" => left ^ right,
                        _ => (BishVariable)BishUtils.Error($"Unsupported operator: {op}"),
                    };
                }
                if (node.ChildNodes.Count == 4 && node.ChildNodes[2].FindTokenAndGetText() == "=") {
                    string type = node.ChildNodes[0].FindTokenAndGetText();
                    bool nullable = node.ChildNodes[0].ChildNodes.Count == 2
                        && node.ChildNodes[0].ChildNodes[1].FindTokenAndGetText() == "?";
                    ParseTreeNode varName = node.ChildNodes[1];
                    BishVariable right = EvaluateSingle(node.ChildNodes[3]);
                    BishVariable value = BishVars.WeakConvert(type, right, nullable);
                    value.nullable = nullable;
                    return vars.New(varName, value);
                }
                return BishUtils.Error($"Unsupported NonTerminal with name {node.Term.Name} and child count of {node.ChildNodes.Count}");
            }
            return BishUtils.Error($"Unsupported expression type: {node.Term.Name}");
        }

        private List<BishVariable> Evaluate(ParseTreeNode node) {
            if (node.ChildNodes.Count == 3 && node.ChildNodes[1].FindTokenAndGetText() == ";") {
                var left = Evaluate(node.ChildNodes[0]);
                var right = Evaluate(node.ChildNodes[2]);
                return [.. left, .. right];
            }
            return [EvaluateSingle(node)];
        }
    }
}