using Irony.Parsing;

namespace Bish {

    internal class BishInterpreter {
        public BishScope scope;
        public BishVars vars;

        public BishInterpreter() {
            scope = new();
            vars = scope.currentVars;
        }

        private void showDepth(string msg) {
            if (Program.ShowVarsStackDepth)
                Console.WriteLine($"{msg} {new string('#', scope.Depth() * 2)}");
        }

        private void Inner() {
            showDepth(">>");
            scope.Inner();
            vars = scope.currentVars;
            showDepth(">>");
        }

        private void Outer() {
            showDepth("<<");
            scope.Outer();
            vars = scope.currentVars;
            showDepth("<<");
        }

        private BishVariable EvaluateInScope(ParseTreeNode parseTree) {
            BishVariable result = new(null);
            Inner();
            try {
                result = Evaluate(parseTree);
            }
            catch (Exception) {
                Outer();
                throw;
            }
            Outer();
            return result;
        }

        public BishVariable Interpret(ParseTree parseTree) {
            if (parseTree.Root == null) return BishUtils.Error("Parse tree is empty.");
            return Evaluate(parseTree.Root);
        }

        private BishVariable Evaluate(ParseTreeNode node) {
            //Console.WriteLine(node.Term.Name);
            if (node == null) return new BishVariable(null);
            else if (node.ChildNodes.Count == 3 && node.ChildNodes[1].FindTokenAndGetText() == ";") {
                Evaluate(node.ChildNodes[0]);
                return Evaluate(node.ChildNodes[2]);
            }
            else if (node.ChildNodes.Count == 3
                && node.ChildNodes[0].FindTokenAndGetText() == "{"
                && node.ChildNodes[2].FindTokenAndGetText() == "}") {
                return EvaluateInScope(node.ChildNodes[1]);
            }
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
                if (node.ChildNodes.Count == 1) return Evaluate(node.ChildNodes[0]);
                if (node.ChildNodes.Count == 2
                    && node.ChildNodes[0].FindTokenAndGetText() == "jump") {
                    string pos = node.ChildNodes[1].FindTokenAndGetText();
                    throw new BishJumpException(pos);
                }
                if (node.ChildNodes.Count == 2 && node.Term.Name == "factor"
                    && node.ChildNodes[0].FindTokenAndGetText().Length == 1) {
                    var sign = node.ChildNodes[0].FindTokenAndGetText();
                    List<string> ops = ["+", "-", "!"];
                    if (ops.Contains(sign)) {
                        var value = Evaluate(node.ChildNodes[1]);
                        return sign switch {
                            "+" => +value,
                            "-" => -value,
                            "!" => !value,
                            _ => BishUtils.Error(),
                        };
                    }
                }
                if (node.ChildNodes.Count == 2 && node.Term.Name == "factor"
                    && node.ChildNodes[1].FindTokenAndGetText().Length == 2) {
                    var sign = node.ChildNodes[1].FindTokenAndGetText();

                    List<string> ops = ["++", "--"];
                    if (ops.Contains(sign)) {
                        var var = vars.Get(node.ChildNodes[0]);
                        if (sign == "++") var++;
                        if (sign == "--") var--;
                        var _ = var; //avoid unused warning
                        return new(null);
                    }
                }
                if (node.ChildNodes.Count == 2 && node.Term.Name == "statement") {
                    var (isConst, type, nullable) = BishVars.CutType(node.ChildNodes[0]);
                    if (isConst && !nullable)
                        BishUtils.Error("Const vars must be Initialized if not nullable");
                    BishVariable var = new(null, null, type, nullable, isConst);
                    return vars.New(node.ChildNodes[1], var);
                }
                if (node.ChildNodes.Count == 3
                    && node.ChildNodes[0].FindTokenAndGetText() == "("
                    && node.ChildNodes[2].FindTokenAndGetText() == ")")
                    return Evaluate(node.ChildNodes[1]);
                if (node.ChildNodes.Count == 3) {
                    string op = node.ChildNodes[1].FindTokenAndGetText();
                    List<string> assignment = ["=", "+=", "-=", "*=", "/=", "%=", "^="];
                    if (assignment.Contains(op)) {
                        var name = node.ChildNodes[0];
                        BishVariable right = Evaluate(node.ChildNodes[2]);
                        return op switch {
                            "=" => vars.Set(name, right),
                            "+=" => vars.Set(name, vars.Get(name) + right),
                            "-=" => vars.Set(name, vars.Get(name) - right),
                            "*=" => vars.Set(name, vars.Get(name) * right),
                            "/=" => vars.Set(name, vars.Get(name) / right),
                            "%=" => vars.Set(name, vars.Get(name) % right),
                            "^=" => vars.Set(name, vars.Get(name) ^ right),
                            _ => BishUtils.Error(),
                        };
                    }
                }
                if (node.ChildNodes.Count == 3) {
                    List<string> shortCircuit = ["&&", "||"];
                    string op = node.ChildNodes[1].FindTokenAndGetText();
                    if (shortCircuit.Contains(op)) {
                        BishVariable left = Evaluate(node.ChildNodes[0]);
                        if (op == "&&") {
                            if (!left.value) return new BishVariable(null, false);
                            else {
                                BishVariable right = Evaluate(node.ChildNodes[2]);
                                return new BishVariable(null, right.value ? true : false);
                            }
                        }
                        else {
                            if (left.value) return new BishVariable(null, true);
                            else {
                                BishVariable right = Evaluate(node.ChildNodes[2]);
                                return new BishVariable(null, right.value ? true : false);
                            }
                        }
                    }
                    else {
                        BishVariable left = Evaluate(node.ChildNodes[0]);
                        BishVariable right = Evaluate(node.ChildNodes[2]);
                        return op switch {
                            "+" => left + right,
                            "-" => left - right,
                            "*" => left * right,
                            "/" => left / right,
                            "%" => left % right,
                            "^" => left ^ right,
                            "<=>" => BishVariable.TriCompare(left, right),
                            "==" => left == right,
                            "!=" => left != right,
                            "<" => left < right,
                            "<=" => left <= right,
                            ">" => left > right,
                            ">=" => left >= right,
                            _ => BishUtils.Error($"Unsupported operator: {op}"),
                        };
                    }
                }
                if (node.ChildNodes.Count == 4
                    && node.ChildNodes[2].FindTokenAndGetText() == "=") {
                    var (isConst, type, nullable) = BishVars.CutType(node.ChildNodes[0]);
                    ParseTreeNode varName = node.ChildNodes[1];
                    BishVariable right = Evaluate(node.ChildNodes[3]);
                    BishVariable value = BishVars.WeakConvert(type, right, nullable);
                    value.nullable = nullable;
                    value.isConst = isConst;
                    return vars.New(varName, value);
                }
                if (node.ChildNodes.Count == 5
                    && node.ChildNodes[1].FindTokenAndGetText() == "?"
                    && node.ChildNodes[3].FindTokenAndGetText() == ":") {
                    BishVariable condition = Evaluate(node.ChildNodes[0]);
                    if (condition.value) return EvaluateInScope(node.ChildNodes[2]);
                    else return EvaluateInScope(node.ChildNodes[4]);
                }
                if (node.ChildNodes.Count == 5
                    && node.ChildNodes[0].FindTokenAndGetText() == "if") {
                    BishVariable condition = Evaluate(node.ChildNodes[2]);
                    if (condition.value) return EvaluateInScope(node.ChildNodes[4]);
                    return new(null);
                }
                if (node.ChildNodes.Count == 5
                    && node.ChildNodes[0].FindTokenAndGetText() == "while") {
                    bool restart = false;
                    BishVariable result = new(null);
                    do {
                        try {
                            while (Evaluate(node.ChildNodes[2]).value)
                                result = EvaluateInScope(node.ChildNodes[4]);
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag != null) throw;
                            if (jump.pos == BishJumpException.Position.START)
                                restart = true;
                            if (jump.pos == BishJumpException.Position.NEXT)
                                BishUtils.Error("Jumping to NEXT is not supported for while loop");
                        }
                    } while (restart);
                    return result;
                }
                if (node.ChildNodes.Count == 5
                    && node.ChildNodes[0].FindTokenAndGetText() == "jump") {
                    string pos = node.ChildNodes[1].FindTokenAndGetText();
                    string tag = node.ChildNodes[3].FindTokenAndGetText();
                    throw new BishJumpException(pos, tag);
                }
                if (node.ChildNodes.Count == 6
                    && node.ChildNodes[1].FindTokenAndGetText() == "while") {
                    string tag = node.ChildNodes[0].ChildNodes[1].FindTokenAndGetText();
                    bool restart = false;
                    BishVariable result = new(null);
                    do {
                        try {
                            while (Evaluate(node.ChildNodes[3]).value)
                                result = EvaluateInScope(node.ChildNodes[5]);
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag != tag && jump.tag != null) throw;
                            if (jump.pos == BishJumpException.Position.START) restart = true;
                            if (jump.pos == BishJumpException.Position.NEXT)
                                BishUtils.Error("Jumping to NEXT is not supported for while loop");
                        }
                    } while (restart);
                    return result;
                }
                if (node.ChildNodes.Count == 6
                    && node.ChildNodes[0].FindTokenAndGetText() == "do") {
                    bool restart = false;
                    BishVariable result = new(null);
                    do {
                        try {
                            do result = EvaluateInScope(node.ChildNodes[1]);
                            while (Evaluate(node.ChildNodes[4]).value);
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag != null) throw;
                            if (jump.pos == BishJumpException.Position.START)
                                restart = true;
                            if (jump.pos == BishJumpException.Position.NEXT)
                                BishUtils.Error("Jumping to NEXT is not supported for do-while loop");
                        }
                    } while (restart);
                    return result;
                }
                if (node.ChildNodes.Count == 7
                    && node.ChildNodes[1].FindTokenAndGetText() == "do") {
                    string tag = node.ChildNodes[0].ChildNodes[1].FindTokenAndGetText();
                    bool restart = false;
                    BishVariable result = new(null);
                    do {
                        try {
                            do result = EvaluateInScope(node.ChildNodes[2]);
                            while (Evaluate(node.ChildNodes[5]).value);
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag != null && jump.tag != tag) throw;
                            if (jump.pos == BishJumpException.Position.START)
                                restart = true;
                            if (jump.pos == BishJumpException.Position.NEXT)
                                BishUtils.Error("Jumping to NEXT is not supported for do-while loop");
                        }
                    } while (restart);
                    return result;
                }
                if (node.ChildNodes.Count == 7
                    && node.ChildNodes[0].FindTokenAndGetText() == "if"
                    && node.ChildNodes[5].FindTokenAndGetText() == "else") {
                    BishVariable condition = Evaluate(node.ChildNodes[2]);
                    if (condition.value) return EvaluateInScope(node.ChildNodes[4]);
                    else return EvaluateInScope(node.ChildNodes[6]);
                }
                if (node.ChildNodes.Count == 9
                    && node.ChildNodes[0].FindTokenAndGetText() == "for") {
                    var init = node.ChildNodes[2];
                    var condition = node.ChildNodes[4];
                    var add = node.ChildNodes[6];
                    bool restart = false;
                    BishVariable result = new(null);
                    do {
                        Inner();
                        try {
                            for (Evaluate(init); Evaluate(condition).value; Evaluate(add)) {
                                try {
                                    result = EvaluateInScope(node.ChildNodes[8]);
                                }
                                catch (BishJumpException jump) {
                                    if (jump.tag != null
                                        || jump.pos != BishJumpException.Position.NEXT) throw;
                                    Evaluate(add);
                                }
                            }
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag != null || jump.pos == BishJumpException.Position.NEXT) throw;
                            if (jump.pos == BishJumpException.Position.START)
                                restart = true;
                        }
                        Outer();
                    } while (restart);
                    return result;
                }
                if (node.ChildNodes.Count == 10
                    && node.ChildNodes[1].FindTokenAndGetText() == "for") {
                    string tag = node.ChildNodes[0].ChildNodes[1].FindTokenAndGetText();
                    var init = node.ChildNodes[3];
                    var condition = node.ChildNodes[5];
                    var add = node.ChildNodes[7];
                    bool restart = false;
                    BishVariable result = new(null);
                    do {
                        Inner();
                        try {
                            for (Evaluate(init); Evaluate(condition).value; Evaluate(add)) {
                                try {
                                    result = EvaluateInScope(node.ChildNodes[8]);
                                }
                                catch (BishJumpException jump) {
                                    if ((jump.tag != null && jump.tag != tag)
                                        || jump.pos != BishJumpException.Position.NEXT) throw;
                                    Evaluate(add);
                                }
                            }
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag != null && jump.tag != tag
                                 || jump.pos == BishJumpException.Position.NEXT) throw;
                            if (jump.pos == BishJumpException.Position.START)
                                restart = true;
                        }
                        Outer();
                    } while (restart);
                    return result;
                }

                if (node.ChildNodes.Count == 4
                    && node.ChildNodes[0].FindTokenAndGetText() == "print") {
                    var value = Evaluate(node.ChildNodes[2]).value;
                    Console.Write(value ?? "null");
                    return new(null);
                }

                return BishUtils.Error($"Unsupported NonTerminal with name {node.Term.Name}"
                    + $" and child count of {node.ChildNodes.Count}");
            }
            return BishUtils.Error($"Unsupported expression type: {node.Term.Name}");
        }
    }
}