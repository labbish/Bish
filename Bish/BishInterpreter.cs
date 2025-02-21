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

        public static BishInterpreter GetInterpreter(BishVars vars) {
            BishInterpreter interpreter = new();
            interpreter.scope = BishScope.GetScope(vars);
            interpreter.vars = interpreter.scope.currentVars;
            return interpreter;
        }

        private void ShowDepth(string msg) {
            if (Program.ShowVarsStackDepth)
                Console.WriteLine($"{msg} {new string('#', scope.Depth() * 2)}");
        }

        private void Inner() {
            ShowDepth(">>");
            scope.Inner();
            vars = scope.currentVars;
            ShowDepth(">>");
        }

        private void Outer() {
            ShowDepth("<<");
            scope.Outer();
            vars = scope.currentVars;
            ShowDepth("<<");
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
            if (parseTree.Root is null) return BishUtils.Error("Parse tree is empty.");
            ClearComments(parseTree.Root);
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

        public static void ClearComments(ParseTreeNode node) {
            List<ParseTreeNode> removes = [];
            foreach (ParseTreeNode child in node.ChildNodes)
                if (child.Term.Name == "singleComment" || child.Term.Name == "multiComment")
                    removes.Add(child);
            foreach (ParseTreeNode remove in removes) node.ChildNodes.Remove(remove);
            foreach (ParseTreeNode child in node.ChildNodes) ClearComments(child);
        }

        public BishVariable Evaluate(ParseTreeNode node, bool isRoot = false) {
            if (isRoot) steps = 0;
            steps++;
            watch.Start();

            if (node is null) return new BishVariable(null);
            if (node.Term is null) return new BishVariable(null);
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
                BishUtils.Assert(str is not null, "NumberLiteral is Null");
                dynamic value;
                if (str!.Contains('.')) value = BishNum.Parse(str!);
                else value = BishInt.Parse(str!);
                return new BishVariable(null, value);
            }
            else if (node.Term is StringLiteral) {
                var str = node.Token.Value.ToString();
                BishUtils.Assert(str is not null, "StringLiteral is Null");
                return new BishVariable(null, str!);
            }
            else if (node.Term.Name == "boolLiteral") {
                var str = node.FindTokenAndGetText();
                BishUtils.Assert(str is not null, "BoolLiteral is Null");
                bool b = str switch {
                    "true" => true,
                    "false" => false,
                    _ => BishUtils.Error($"{str} is not bool"),
                };
                return new BishVariable(null, b);
            }
            else if (node.Term.Name == "varModifiedTypes") {
                BishTypeInfo type = EvaluateType(node);
                return new(null, value: type, typeName: "type");
            }
            else if (node.Term.Name == "null") {
                return new BishVariable(null);
            }
            else if (node.Term.Name == "inf") {
                return new BishVariable(null, BishInt.Inf);
            }
            else if (node.ChildNodes.Count == 0
                && node.FindTokenAndGetText() == "return")
                throw new BishReturnException(new(null));
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
                    BishTypeInfo type = Evaluate(node.ChildNodes[0]).value!;
                    BishVariable var = new(name: null, type: type, null);
                    if (var.type.isConst && !var.type.nullable)
                        BishUtils.Error("Const vars must be Initialized if not nullable");
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
                        var newNode = GetNewNode();
                        newNode.ChildNodes.Add(node.ChildNodes[0]);
                        newNode.ChildNodes.Add(node.ChildNodes[2]);
                        newNode.ChildNodes.Add(node.ChildNodes[3]);
                        return !Evaluate(newNode);
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
                            "&" => left & right,
                            "|" => left | right,
                            _ => BishUtils.Error($"Unsupported operator: {op}"),
                        };
                    }
                }
                if (node.ChildNodes.Count == 4
                    && node.ChildNodes[2].FindTokenAndGetText() == "=") {
                    BishTypeInfo type = Evaluate(node.ChildNodes[0]).value!;
                    ParseTreeNode varName = node.ChildNodes[1];
                    BishVariable right = Evaluate(node.ChildNodes[3]);
                    BishVariable value = BishVars.WeakConvert(type, right);
                    return vars.New(varName, value);
                }
                if (node.ChildNodes.Count == 4
                    && node.ChildNodes[1].FindTokenAndGetText() == "("
                    && node.ChildNodes[3].FindTokenAndGetText() == ")") {
                    if (node.ChildNodes[0].Term.Name == "identifier") {
                        List<BishInArg> args = [];
                        if (node.ChildNodes[2].ChildNodes.Count != 0)
                            args = [.. ToPlainArgs(node.ChildNodes[2].ChildNodes[0])
                            .Select(arg => EvaluateArg(arg))];
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
                        List<BishInArg> args = [];
                        if (node.ChildNodes[2].ChildNodes.Count != 0)
                            args = [.. ToPlainArgs(node.ChildNodes[2].ChildNodes[0])
                            .Select(arg => EvaluateArg(arg))];
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
                    BishUtils.Assert(left is not null && right is not null, $"Wrong Interval");
                    BishNum from = (BishNum)Evaluate(node.ChildNodes[1]).value!;
                    BishNum to = (BishNum)Evaluate(node.ChildNodes[3]).value!;
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
                                    if ((jump.tag is not null)
                                        || jump.pos != BishJumpException.Position.NEXT) throw;
                                }
                            }
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag is not null) throw;
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
                                    if ((jump.tag is not null && jump.tag != tag)
                                        || jump.pos != BishJumpException.Position.NEXT) throw;
                                }
                            }
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag != tag && jump.tag is not null) throw;
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
                                    if (jump.tag is not null
                                        || jump.pos != BishJumpException.Position.NEXT) throw;
                                }
                            }
                            while (Evaluate(node.ChildNodes[4]).value);
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag is not null) throw;
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
                                    if ((jump.tag is not null && jump.tag != tag)
                                        || jump.pos != BishJumpException.Position.NEXT) throw;
                                }
                            }
                            while (Evaluate(node.ChildNodes[5]).value);
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag is not null && jump.tag != tag) throw;
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
                                    if (jump.tag is not null
                                        || jump.pos != BishJumpException.Position.NEXT) throw;
                                    Evaluate(add);
                                }
                            }
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag is not null || jump.pos == BishJumpException.Position.NEXT) throw;
                            if (jump.pos == BishJumpException.Position.START)
                                restart = true;
                        }
                        finally {
                            Outer();
                        }
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
                                    if ((jump.tag is not null && jump.tag != tag)
                                        || jump.pos != BishJumpException.Position.NEXT) throw;
                                    Evaluate(add);
                                }
                            }
                        }
                        catch (BishJumpException jump) {
                            if (jump.tag is not null && jump.tag != tag
                                 || jump.pos == BishJumpException.Position.NEXT) throw;
                            if (jump.pos == BishJumpException.Position.START)
                                restart = true;
                        }
                        finally {
                            Outer();
                        }
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
                    && node.ChildNodes[0].Term.Name == "funcStateType") {
                    var (isConst, returnType, decorators) = EvaluateFuncType(node.ChildNodes[0]);
                    var args = ToPlainArgs(node.ChildNodes[2])
                        .Select(ToBishArg).ToList();
                    Inner();
                    var f = node.ChildNodes[4];
                    ParseTreeNode? where = null;
                    if (node.ChildNodes[3].ChildNodes.Count == 5)
                        where = node.ChildNodes[3].ChildNodes[3];
                    BishFunc func;
                    if (f.ChildNodes.Count == 1) func = new(vars, f, args, where: where);
                    else func = new(vars, f.ChildNodes[1], args, where: where);
                    foreach (BishVariable decorator in decorators) {
                        func = decorator.Exec([new(new(null, func))]).value!;
                    }
                    Outer();
                    return new(null, func);
                }
                if (node.ChildNodes.Count == 6
                    && node.ChildNodes[0].Term.Name == "defStateType") {
                    var (isConst, returnType, decorators) = EvaluateFuncType(node.ChildNodes[0]);
                    var args = ToPlainArgs(node.ChildNodes[3])
                        .Select(ToBishArg).ToList();
                    Inner();
                    var f = node.ChildNodes[5];
                    ParseTreeNode? where = null;
                    if (node.ChildNodes[4].ChildNodes.Count == 5)
                        where = node.ChildNodes[4].ChildNodes[3];
                    BishFunc func;
                    if (f.ChildNodes.Count == 1) func = new(vars, f, args, returnType, where);
                    else func = new(vars, f.ChildNodes[1], args, returnType, where);
                    Outer();
                    BishVariable newFunc = vars.New(node.ChildNodes[1], new(null, type: new(func,
                        typeArgs: returnType is null ? [] : [new(null, value: returnType)],
                        isConst: isConst), func), checkExist: false);
                    func.BindSelf(node.ChildNodes[1].FindTokenAndGetText(), newFunc);
                    foreach (BishVariable decorator in decorators) {
                        vars.Set(node.ChildNodes[1],
                            decorator.Exec([new(newFunc)]), checkConst: false);
                        newFunc = vars.Get(node.ChildNodes[1]);
                    }
                    return newFunc;
                }

                if (node.ChildNodes.Count == 5
                    && node.ChildNodes[0].FindTokenAndGetText() == "class") {
                    string name = node.ChildNodes[1].FindTokenAndGetText();
                    BishType type = new(name);
                    EvaluateInClass(type, node.ChildNodes[3]);
                    BishVariable typeVar = new(name, value: type);
                    vars.New(name, typeVar);
                    return vars.Get(name);
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
                var newNode = GetNewNode();
                newNode.ChildNodes.Add(node.ChildNodes[0]);
                newNode.ChildNodes.Add(node.ChildNodes[1]);
                newNode.ChildNodes.Add(expr.ChildNodes[1]);
                return !EvaluateMatching(newNode, expr.ChildNodes[1]);
            }
            if (expr.ChildNodes.Count == 2
                  && expr.ChildNodes[0].FindTokenAndGetText() == "func") {
                BishVariable ans;
                if (expr.ChildNodes[1].Term.Name == "identifier")
                    ans = vars.Exec(expr.ChildNodes[1], [new(Evaluate(node.ChildNodes[0]))]);
                else {
                    var func = Evaluate(expr.ChildNodes[1]);
                    ans = func.Exec([new(Evaluate(node.ChildNodes[0]))]);
                }
                return BishVars.WeakConvert(new(type: "bool"), ans);
            }
            if (expr.ChildNodes.Count == 2
                  && BishGrammar.MatchableOperators.Contains(expr.ChildNodes[0].FindTokenAndGetText())) {
                var newNode = GetNewNode();
                newNode.ChildNodes.Add(node.ChildNodes[0]);
                newNode.ChildNodes.Add(expr.ChildNodes[0]);
                newNode.ChildNodes.Add(expr.ChildNodes[1]);
                //node.ChildNodes[1] = expr.ChildNodes[0];
                //node.ChildNodes[2] = expr.ChildNodes[1];
                return Evaluate(newNode);
            }
            else if (expr.ChildNodes.Count == 2) {
                BishVariable value = Evaluate(node.ChildNodes[0]);
                BishTypeInfo type = Evaluate(expr.ChildNodes[0]).value!;
                BishVariable converted;
                try {
                    converted = BishVars.WeakConvert(type, value);
                }
                catch (ArgumentException) {
                    return new(null, false);
                }
                vars.New(expr.ChildNodes[1], converted);
                return new(null, true);
            }
            else if (expr.ChildNodes.Count == 3
                && expr.ChildNodes[0].FindTokenAndGetText() == "("
                && expr.ChildNodes[2].FindTokenAndGetText() == ")") {
                return EvaluateMatching(node, expr.ChildNodes[1]);
            }
            else if (expr.ChildNodes.Count == 3
                && expr.ChildNodes[1].FindTokenAndGetText() == "and") {
                var left = EvaluateMatching(node, expr.ChildNodes[0]);
                if (!left.value) return new(null, false);
                return EvaluateMatching(node, expr.ChildNodes[2]);
            }
            else if (expr.ChildNodes.Count == 3
                && expr.ChildNodes[1].FindTokenAndGetText() == "or") {
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
                BishTypeInfo type = Evaluate(node.ChildNodes[0]).value!;
                string name = node.ChildNodes[1].FindTokenAndGetText();
                return new(type, name);
            }
            if (node.ChildNodes.Count == 4
                && node.ChildNodes[2].FindTokenAndGetText() == "=") {
                BishTypeInfo type = Evaluate(node.ChildNodes[0]).value!;
                string name = node.ChildNodes[1].FindTokenAndGetText();
                BishVariable defaultValue = Evaluate(node.ChildNodes[3]);
                return new(type, name, defaultValue);
            }
            return BishUtils.Error("Error Arg");
        }

        private BishInArg EvaluateArg(ParseTreeNode node) {
            if (node.ChildNodes.Count == 1
                && node.Term.Name != "assignment")
                return EvaluateArg(node.ChildNodes[0]);
            if (node.ChildNodes.Count == 3
                && node.ChildNodes[1].FindTokenAndGetText() == ":")
                return new(node.ChildNodes[0].FindTokenAndGetText(),
                    Evaluate(node.ChildNodes[2]));
            return new(Evaluate(node));
        }

        private BishTypeInfo EvaluateType(ParseTreeNode node) {
            if (node.Term.Name == "varOriginalTypes")
                return new(type: node.FindTokenAndGetText());
            if (node.ChildNodes.Count == 1) return EvaluateType(node.ChildNodes[0]);
            if (node.ChildNodes.Count == 2
                && node.ChildNodes[0].FindTokenAndGetText() == "const") {
                BishTypeInfo sub = EvaluateType(node.ChildNodes[1]);
                sub.isConst = true;
                return sub;
            }
            if (node.ChildNodes.Count == 2
                && node.ChildNodes[1].FindTokenAndGetText() == "?") {
                BishTypeInfo sub = EvaluateType(node.ChildNodes[0]);
                sub.nullable = true;
                return sub;
            }
            if (node.ChildNodes.Count == 4
                && node.ChildNodes[1].FindTokenAndGetText() == "["
                && node.ChildNodes[3].FindTokenAndGetText() == "]") {
                BishTypeInfo sub = EvaluateType(node.ChildNodes[0]);
                List<ParseTreeNode> args = ToPlainArgs(node.ChildNodes[2]);
                List<BishVariable> typeArgs =
                    [.. args.Select(EvaluateArg).Select(arg => arg.value)];
                sub.typeArgs = typeArgs;
                sub.Simplify();
                return sub;
            }
            return BishUtils.Error("Cannot Evaluate Type");
        }

        private (bool isConst, BishTypeInfo? returnType, List<BishVariable> decorators)
            EvaluateFuncType(ParseTreeNode node) {
            if (node.ChildNodes.Count == 0
                && (node.Term.Name == "def" || node.Term.Name == "func")) {
                return (false, null, []);
            }
            if (node.ChildNodes.Count == 1) {
                return EvaluateFuncType(node.ChildNodes[0]);
            }
            if (node.ChildNodes.Count == 2
                && node.ChildNodes[0].FindTokenAndGetText() == "const") {
                var (_, returnType, decorators) = EvaluateFuncType(node.ChildNodes[1]);
                return (true, returnType, decorators);
            }
            if (node.ChildNodes.Count == 2
                && node.ChildNodes[0].Term.Name == "decorator") {
                var (isConst, returnType, decorators) = EvaluateFuncType(node.ChildNodes[1]);
                BishVariable decorator = Evaluate(node.ChildNodes[0].ChildNodes[1]);
                return (isConst, returnType, [.. decorators.Concat([decorator])]);
            }
            if (node.ChildNodes.Count == 4
                && node.ChildNodes[1].FindTokenAndGetText() == "["
                && node.ChildNodes[3].FindTokenAndGetText() == "]") {
                var (isConst, _, decorators) = EvaluateFuncType(node.ChildNodes[0]);
                BishTypeInfo? returnType = Evaluate(node.ChildNodes[2]).value;
                return (isConst, returnType, decorators);
            }
            return BishUtils.Error("Wrong Func Type");
        }

        private void EvaluateInClass(BishType type, ParseTreeNode node) {
            var interpreter = GetInterpreter(type.members);
            if (node.ChildNodes.Count == 0) return;
            else if (node.ChildNodes.Count == 1) EvaluateInClass(type, node.ChildNodes[0]);
            else if (node.ChildNodes.Count == 3
                && node.ChildNodes[1].FindTokenAndGetText() == ";") {
                EvaluateInClass(type, node.ChildNodes[0]);
                EvaluateInClass(type, node.ChildNodes[2]);
            }
            else if (node.Term.Name == "classVarStatement") {
                var newNode = CopyNode(node, "statement");
                interpreter.Evaluate(newNode);
            }
            else BishUtils.Error("In-Class Expression Not Supported");
            //BishUtils.NotImplemented();
        }

        public static ParseTreeNode GetNewNode(string name = "") {
            Type type = typeof(ParseTreeNode);
            ConstructorInfo? constructor =
                type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, [], []);
            if (constructor is null) return BishUtils.Error("Constructor not Found");
            var node = (ParseTreeNode)constructor.Invoke(null);
            node.Term = new NonTerminal(name);
            return node;
        }

        public static ParseTreeNode CopyNode(ParseTreeNode from, string name = "") {
            var node = GetNewNode(name);
            foreach (var child in from.ChildNodes) node.ChildNodes.Add(child);
            return node;
        }
    }
}