using Irony.Parsing;
using System.Diagnostics;
using System.Reflection;

namespace Bish {

    internal class BishInterpreter {
        public BishScope scope;
        public BishVars vars;
        private int steps;
        private Stopwatch watch = new();

        public BishInterpreter() {
            scope = new();
            vars = scope.currentVars;
        }

        public BishInterpreter(BishVars vars) {
            scope = new(vars);
            this.vars = scope.currentVars;
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
                throw;
            }
            finally {
                Outer();
            }
            return result;
        }

        public BishVariable Interpret(ParseTree parseTree) {
            if (parseTree.Root == null) return BishUtils.Error("Parse tree is empty.");
            var ans = Evaluate(parseTree.Root, true);
            watch.Stop();
            if (Program.ShowEvaluateSteps)
                Console.WriteLine($"Complete Evaluation after {steps} Steps");
            if (Program.ShowEvaluateTime)
                Console.WriteLine($"Cost {watch.Elapsed.TotalMilliseconds:N2} ms, "
                    + $"Average Cost {watch.Elapsed.TotalMilliseconds / steps:N2} ms");
            watch.Reset();
            return ans;
        }

        public BishVariable Evaluate(ParseTreeNode node, bool isRoot = false) {
            if (isRoot) steps = 0;
            steps++;
            watch.Start();

            if (node == null) return new BishVariable(null);
            if (node.Term == null) return new BishVariable(null);
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
            else if (node.Term.Name == "inf") {
                return new BishVariable(null, double.PositiveInfinity);
            }
            else if (node.Term is NonTerminal) {
                if (node.ChildNodes.Count == 0) return new BishVariable(null);
                if (node.ChildNodes.Count == 1
                    && node.ChildNodes[0].FindTokenAndGetText() == "continue")
                    throw new BishContinueException();
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
                if (node.ChildNodes.Count == 2
                    && node.ChildNodes[0].FindTokenAndGetText() == "return") {
                    BishVariable value = Evaluate(node.ChildNodes[1]);
                    throw new BishReturnException(value);
                }
                if (node.ChildNodes.Count == 2 && node.Term.Name == "statement") {
                    var (isConst, type, nullable) = BishVars.CutType(node.ChildNodes[0]);
                    if (isConst && !nullable)
                        BishUtils.Error("Const vars must be Initialized if not nullable");
                    BishVariable var = new(null, null, type, nullable, isConst);
                    return vars.New(node.ChildNodes[1], var);
                }
                if (node.ChildNodes.Count(node => node.FindTokenAndGetText() == "~") == 1) {
                    if (node.ChildNodes.Count == 3
                        && node.ChildNodes[1].FindTokenAndGetText() == "~") {
                        var expr = node.ChildNodes[2];
                        return EvaluateMatching(node, expr);
                    }
                    if (node.ChildNodes.Count == 4
                        && node.ChildNodes[1].FindTokenAndGetText() == "!"
                        && node.ChildNodes[2].FindTokenAndGetText() == "~") {
                        node.ChildNodes.RemoveAt(1);
                        return !Evaluate(node);
                    }
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
                    value.isConst = isConst;
                    return vars.New(varName, value);
                }
                if (node.ChildNodes.Count == 4
                    && node.ChildNodes[1].FindTokenAndGetText() == "("
                    && node.ChildNodes[3].FindTokenAndGetText() == ")") {
                    if (node.ChildNodes[0].Term.Name == "identifier") {
                        List<BishVariable> args = [];
                        if (node.ChildNodes[2].ChildNodes.Count != 0)
                            args = [.. ToPlainArgs(node.ChildNodes[2].ChildNodes[0])
                            .Select(arg => Evaluate(arg))];
                        BishVariable value;
                        try {
                            value = vars.Exec(node.ChildNodes[0], [.. args]);
                        }
                        catch (BishReturnException returning) {
                            value = returning.returnVar;
                        }
                        return value;
                    }
                    else {
                        List<BishVariable> args = [];
                        if (node.ChildNodes[2].ChildNodes.Count != 0)
                            args = [.. ToPlainArgs(node.ChildNodes[2].ChildNodes[0])
                            .Select(arg => Evaluate(arg))];
                        BishVariable value;
                        try {
                            var func = Evaluate(node.ChildNodes[0]);
                            value = func.Exec([.. args]);
                        }
                        catch (BishReturnException returning) {
                            value = returning.returnVar;
                        }
                        return value;
                    }
                }
                if (node.ChildNodes.Count == 5
                && node.ChildNodes[2].FindTokenAndGetText() == ",") {
                    bool? left = node.ChildNodes[0].FindTokenAndGetText() switch {
                        "[" => true,
                        "(" => false,
                        _ => null,
                    };
                    bool? right = node.ChildNodes[4].FindTokenAndGetText() switch {
                        "]" => true,
                        ")" => false,
                        _ => null,
                    };
                    BishUtils.Assert(left != null && right != null, $"Wrong Interval");
                    double from = (double)Evaluate(node.ChildNodes[1]).value;
                    double to = (double)Evaluate(node.ChildNodes[3]).value;
                    return new(null, new BishInterval(left!.Value, from, right!.Value, to), "interval");
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
                    && node.ChildNodes[0].FindTokenAndGetText() == "jump") {
                    string pos = node.ChildNodes[1].FindTokenAndGetText();
                    string tag = node.ChildNodes[3].FindTokenAndGetText();
                    throw new BishJumpException(pos, tag);
                }
                if (node.ChildNodes.Count == 5
                    && node.ChildNodes[0].FindTokenAndGetText() == "while") {
                    bool restart = false;
                    BishVariable result = new(null);
                    do {
                        try {
                            while (Evaluate(node.ChildNodes[2]).value) {
                                try {
                                    result = EvaluateInScope(node.ChildNodes[4]);
                                }
                                catch (BishJumpException jump) {
                                    if ((jump.tag != null)
                                        || jump.pos != BishJumpException.Position.NEXT) throw;
                                }
                            }
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag != null) throw;
                            if (jump.pos == BishJumpException.Position.START)
                                restart = true;
                        }
                    } while (restart);
                    return result;
                }
                if (node.ChildNodes.Count == 6
                    && node.ChildNodes[1].FindTokenAndGetText() == "while") {
                    string tag = node.ChildNodes[0].ChildNodes[1].FindTokenAndGetText();
                    bool restart = false;
                    BishVariable result = new(null);
                    do {
                        try {
                            while (Evaluate(node.ChildNodes[3]).value) {
                                try {
                                    result = EvaluateInScope(node.ChildNodes[5]);
                                }
                                catch (BishJumpException jump) {
                                    if ((jump.tag != null && jump.tag != tag)
                                        || jump.pos != BishJumpException.Position.NEXT) throw;
                                }
                            }
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag != tag && jump.tag != null) throw;
                            if (jump.pos == BishJumpException.Position.START) restart = true;
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
                            do {
                                try {
                                    result = EvaluateInScope(node.ChildNodes[1]);
                                }
                                catch (BishJumpException jump) {
                                    if (jump.tag != null
                                        || jump.pos != BishJumpException.Position.NEXT) throw;
                                }
                            }
                            while (Evaluate(node.ChildNodes[4]).value);
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag != null) throw;
                            if (jump.pos == BishJumpException.Position.START)
                                restart = true;
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
                            do {
                                try {
                                    result = EvaluateInScope(node.ChildNodes[2]);
                                }
                                catch (BishJumpException jump) {
                                    if ((jump.tag != null && jump.tag != tag)
                                        || jump.pos != BishJumpException.Position.NEXT) throw;
                                }
                            }
                            while (Evaluate(node.ChildNodes[5]).value);
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag != null && jump.tag != tag) throw;
                            if (jump.pos == BishJumpException.Position.START)
                                restart = true;
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
                                    result = EvaluateInScope(node.ChildNodes[9]);
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

                if (node.ChildNodes.Count == 7
                    && node.ChildNodes[0].FindTokenAndGetText() == "switch") {
                    var value = node.ChildNodes[2];
                    var cases = node.ChildNodes[5];
                    var caseBlocks = ToPlainCaseBlocks(cases)
                        .Select(single =>
                        (Match: single.ChildNodes[0].FindTokenAndGetText() == "default"
                        ? null : single.ChildNodes[0].ChildNodes[1],
                        Block: single.ChildNodes[1]))
                        .ToList();
                    return EvaluateSwitch(value, caseBlocks);
                }

                if (node.ChildNodes.Count == 5
                    && node.ChildNodes[0].FindTokenAndGetText() == "func") {
                    var args = ToPlainArgs(node.ChildNodes[2])
                        .Select(ToBishArg).ToList();
                    Inner();
                    var f = node.ChildNodes[4];
                    BishFunc func;
                    if (f.ChildNodes.Count == 1) func = new(vars, f, args);
                    else func = new(vars, f.ChildNodes[1], args);
                    Outer();
                    return new(null, func);
                }
                if (node.ChildNodes.Count == 6
                    && node.ChildNodes[0].FindTokenAndGetText() == "def") {
                    var args = ToPlainArgs(node.ChildNodes[3])
                        .Select(ToBishArg).ToList();
                    Inner();
                    var f = node.ChildNodes[5];
                    BishFunc func;
                    if (f.ChildNodes.Count == 1) func = new(vars, f, args);
                    else func = new(vars, f.ChildNodes[1], args);
                    Outer();
                    return vars.NewUnchecked(node.ChildNodes[1], new(null, func));
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

        private BishVariable EvaluateMatching(ParseTreeNode node, ParseTreeNode expr) {
            while (expr.ChildNodes.Count == 1) {
                var left = Evaluate(node.ChildNodes[0]);
                BishVariable right;
                try {
                    right = Evaluate(expr);
                }
                catch (Exception) {
                    expr = expr.ChildNodes[0];
                    continue;
                }
                if (right.value is null) return new(null, left.value is null);
                else return left == right;
            }
            if (expr.ChildNodes.Count == 2
                  && expr.ChildNodes[0].FindTokenAndGetText() == "not") {
                node.ChildNodes[2] = expr.ChildNodes[1];
                return !EvaluateMatching(node, expr.ChildNodes[1]);
            }
            if (expr.ChildNodes.Count == 2
                  && BishGrammar.MatchableOperators.Contains(expr.ChildNodes[0].FindTokenAndGetText())) {
                node.ChildNodes[1] = expr.ChildNodes[0];
                node.ChildNodes[2] = expr.ChildNodes[1];
                return Evaluate(node);
            }
            else if (expr.ChildNodes.Count == 2) {
                BishVariable value = Evaluate(node.ChildNodes[0]);
                var (isConst, type, nullable) = BishVars.CutType(expr.ChildNodes[0]);
                BishVariable converted;
                try {
                    converted = BishVars.WeakConvert(type, value, nullable);
                }
                catch (ArgumentException) {
                    return new(null, false);
                }
                converted.isConst = isConst;
                vars.New(expr.ChildNodes[1], converted);
                return new(null, true);
            }
            else if (expr.ChildNodes.Count == 3
                && expr.ChildNodes[0].FindTokenAndGetText() == "("
                && expr.ChildNodes[2].FindTokenAndGetText() == ")") {
                return EvaluateMatching(node, expr.ChildNodes[1]);
            }
            else if (expr.ChildNodes.Count == 3
                && expr.ChildNodes[1].FindTokenAndGetText() == "&") {
                var left = EvaluateMatching(node, expr.ChildNodes[0]);
                if (!left.value) return new(null, false);
                return EvaluateMatching(node, expr.ChildNodes[2]);
            }
            else if (expr.ChildNodes.Count == 3
                && expr.ChildNodes[1].FindTokenAndGetText() == "|") {
                var left = EvaluateMatching(node, expr.ChildNodes[0]);
                if (left.value) return new(null, true);
                return EvaluateMatching(node, expr.ChildNodes[2]);
            }
            return BishUtils.Error($"Wrong Matching Pattern: {BishVars.ToPlainString(expr)}");
        }

        private BishVariable EvaluateSwitch(ParseTreeNode value,
            List<(ParseTreeNode? Match, ParseTreeNode Block)> caseBlocks,
            bool reverse = false) {
            if (value.ChildNodes.Count == 2
                && value.ChildNodes[1].FindTokenAndGetText() == "!")
                return EvaluateSwitch(value.ChildNodes[0], caseBlocks, !reverse);

            foreach (var (match, block) in caseBlocks) {
                Inner();
                ParseTreeNode equal = GetNewNode("matching");
                equal.ChildNodes.Add(value);
                equal.ChildNodes.Add(GetNewNode());
                equal.ChildNodes.Add(match);
                bool done = false;
                BishVariable result = new(null);
                try {
                    bool condition;
                    if (match is null) condition = true;
                    else condition = EvaluateMatching(equal, match).value;
                    if (condition ^ reverse) (done, result) = (true, EvaluateInScope(block));
                }
                catch (BishContinueException) {
                    done = false;
                }
                catch (Exception) {
                    throw;
                }
                finally {
                    Outer();
                }
                if (done) return result;
            }
            return new(null);
        }

        private static List<ParseTreeNode> ToPlainCaseBlocks(ParseTreeNode cases) {
            if (cases.Term.Name == "caseBlock") return [cases];
            return [.. cases.ChildNodes.SelectMany(son => ToPlainCaseBlocks(son))];
        }

        private static List<ParseTreeNode> ToPlainArgs(ParseTreeNode node) {
            if (node.Term.Name == "funcCallArg"
                || node.Term.Name == "funcStateArg") return [node];
            if (node.ChildNodes.Count == 0) return [];
            if (node.ChildNodes.Count == 1) return ToPlainArgs(node.ChildNodes[0]);
            if (node.ChildNodes.Count == 3
                && node.ChildNodes[1].FindTokenAndGetText() == ",")
                return [.. ToPlainArgs(node.ChildNodes[0]), .. ToPlainArgs(node.ChildNodes[2])];
            return [node];
        }

        private BishArg ToBishArg(ParseTreeNode node) {
            if (node.ChildNodes.Count == 1) return ToBishArg(node.ChildNodes[0]);
            if (node.ChildNodes.Count == 2) {
                var type = node.ChildNodes[0];
                string name = node.ChildNodes[1].FindTokenAndGetText();
                return new(type, name);
            }
            if (node.ChildNodes.Count == 4
                && node.ChildNodes[2].FindTokenAndGetText() == "=") {
                var type = node.ChildNodes[0];
                string name = node.ChildNodes[1].FindTokenAndGetText();
                BishVariable defaultValue = Evaluate(node.ChildNodes[3]);
                return new(type, name, defaultValue);
            }
            return BishUtils.Error("Error Arg");
        }

        public static ParseTreeNode GetNewNode(string name = "") {
            Type type = typeof(ParseTreeNode);
            ConstructorInfo? constructor =
                type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, [], []);
            if (constructor == null) return BishUtils.Error("Constructor not Found");
            var node = (ParseTreeNode)constructor.Invoke(null);
            node.Term = new NonTerminal(name);
            return node;
        }
    }
}