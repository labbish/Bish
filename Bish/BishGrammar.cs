using Irony.Parsing;

namespace Bish {

    public class BishGrammar : Grammar {
        public static readonly List<string> MatchableOperators = ["==", "!=", "<", "<=", ">", ">="];

        public BishGrammar() {
            var identifier = new IdentifierTerminal("identifier");
            var numberLiteral = new NumberLiteral("numberLiteral");
            var singleString = new StringLiteral("single_string", "'");
            var doubleString = new StringLiteral("double_string", "\"");

            var intType = ToTerm("int");
            var numType = ToTerm("num");
            var stringType = ToTerm("string");
            var boolType = ToTerm("bool");
            var trueLiteral = ToTerm("true");
            var falseLiteral = ToTerm("false");
            var nullLiteral = ToTerm("null");
            var constModifier = ToTerm("const");
            var ifTerm = ToTerm("if");
            var elseTerm = ToTerm("else");
            var whileTerm = ToTerm("while");
            var doTerm = ToTerm("do");
            var forTerm = ToTerm("for");
            var jumpTerm = ToTerm("jump");
            var endPos = ToTerm("end");
            var startPos = ToTerm("start");
            var nextPos = ToTerm("next");
            var tagTerm = ToTerm("tag");
            var intervalType = ToTerm("interval");
            var infLiteral = ToTerm("inf");
            var switchTerm = ToTerm("switch");
            var caseTerm = ToTerm("case");
            var continueTerm = ToTerm("continue");
            var defaultTerm = ToTerm("default");
            var printTerm = ToTerm("print"); //TEMP

            var stringLiteral = new NonTerminal("stringLiteral");
            var boolLiteral = new NonTerminal("boolLiteral");
            var literal = new NonTerminal("literal");
            var factor = new NonTerminal("factor");
            var powerExpr = new NonTerminal("powerExpr");
            var term = new NonTerminal("term");
            var expression = new NonTerminal("expression");
            var comparison = new NonTerminal("comparison");
            var logicAnd = new NonTerminal("logicAnd");
            var logicOr = new NonTerminal("logicOr");
            var triCondition = new NonTerminal("triCondition");
            var assignment = new NonTerminal("assignment");
            var interval = new NonTerminal("interval");
            var varTypes = new NonTerminal("varTypes");
            var varNullableTypes = new NonTerminal("varNullableTypes");
            var varModifiedTypes = new NonTerminal("varModifiedTypes");
            var matchingExpr = new NonTerminal("matchingExpr");
            var matchingAndExpr = new NonTerminal("matchingAndExpr");
            var matchingOrExpr = new NonTerminal("matchingOrExpr");
            var matching = new NonTerminal("matching");
            var statement = new NonTerminal("statement");
            var jumpPos = new NonTerminal("jumpPos");
            var jump = new NonTerminal("jump");
            var sentence = new NonTerminal("sentence");
            var sentences = new NonTerminal("sentences");
            var structure = new NonTerminal("structure");
            var codeBlocks = new NonTerminal("codeBlocks");
            var ifStatement = new NonTerminal("ifStatement");
            var tag = new NonTerminal("tag");
            var loopStatement = new NonTerminal("loopStatement");
            var caseTag = new NonTerminal("caseTag");
            var caseBlock = new NonTerminal("caseBlock");
            var caseBlocks = new NonTerminal("caseBlocks");
            var switchExpr = new NonTerminal("switchExpr");
            var switchStatement = new NonTerminal("switchStatement");
            var print = new NonTerminal("print");
            var root = new NonTerminal("root");

            stringLiteral.Rule = singleString | doubleString;
            boolLiteral.Rule = trueLiteral | falseLiteral;
            literal.Rule = stringLiteral | numberLiteral | boolLiteral
                | nullLiteral | infLiteral | interval;
            factor.Rule = "!" + factor
                | "+" + factor | "-" + factor | factor + "++" | factor + "--"
                | literal | identifier | "(" + codeBlocks + ")";
            powerExpr.Rule = factor | powerExpr + "^" + factor;
            term.Rule = powerExpr | term + "*" + powerExpr | term + "/" + powerExpr
                | term + "%" + powerExpr;
            expression.Rule = term | expression + "+" + term | expression + "-" + term;
            comparison.Rule = expression | comparison + "<=>" + expression
                | comparison + "==" + expression | comparison + "!=" + expression
                | comparison + ">" + expression | comparison + ">=" + expression
                | comparison + "<" + expression | comparison + "<=" + expression;
            logicAnd.Rule = comparison | logicAnd + "&&" + comparison;
            logicOr.Rule = logicAnd | logicOr + "||" + logicAnd;
            triCondition.Rule = logicOr | logicOr + "?" + codeBlocks + ":" + codeBlocks;
            assignment.Rule = triCondition | identifier + "=" + assignment
                | identifier + "+=" + assignment | identifier + "-=" + assignment
                | identifier + "*=" + assignment | identifier + "/=" + assignment
                | identifier + "%=" + assignment | identifier + "^=" + assignment;
            interval.Rule = "(" + assignment + "," + assignment + ")"
                | "(" + assignment + "," + assignment + "]"
                | "[" + assignment + "," + assignment + ")"
                | "[" + assignment + "," + assignment + "]";
            varTypes.Rule = intType | numType | stringType | boolType | intervalType;
            varNullableTypes.Rule = varTypes | varTypes + "?";
            varModifiedTypes.Rule = varNullableTypes | constModifier + varNullableTypes;
            matchingExpr.Rule = assignment | varNullableTypes + identifier;
            foreach (var op in MatchableOperators) matchingExpr.Rule |= op + assignment;
            matchingExpr.Rule |= "(" + matchingOrExpr + ")";
            matchingAndExpr.Rule = matchingExpr | matchingAndExpr + "&" + matchingExpr;
            matchingOrExpr.Rule = matchingAndExpr | matchingOrExpr + "|" + matchingAndExpr;
            matching.Rule = assignment | assignment + "~" + matchingOrExpr
                | assignment + "!" + "~" + matchingOrExpr;
            statement.Rule = matching | varModifiedTypes + identifier
                | varModifiedTypes + identifier + "=" + matching;
            jumpPos.Rule = endPos | startPos | nextPos;
            jump.Rule = jumpTerm + jumpPos + "[" + identifier + "]" | jumpTerm + jumpPos;
            sentence.Rule = Empty | statement | jump | continueTerm;
            sentences.Rule = root | sentence | sentences + ";" + root;
            structure.Rule = "{" + sentences + "}";
            codeBlocks.Rule = sentences | "{" + sentences + "}";
            ifStatement.Rule = codeBlocks | ifTerm + "(" + sentence + ")" + structure
                | ifTerm + "(" + sentence + ")" + structure + elseTerm + structure;
            tag.Rule = tagTerm + identifier + ":";
            loopStatement.Rule = codeBlocks
                | whileTerm + "(" + sentence + ")" + structure
                | tag + whileTerm + "(" + sentence + ")" + structure
                | doTerm + structure + whileTerm + "(" + sentence + ")"
                | tag + doTerm + structure + whileTerm + "(" + sentence + ")"
                | forTerm + "(" + sentence + ";" + sentence + ";" + sentence + ")" + structure
                | tag + forTerm + "(" + sentence + ";" + sentence + ";" + sentence + ")" + structure;
            caseTag.Rule = caseTerm + matchingOrExpr + ":" | defaultTerm + ":";
            caseBlock.Rule = caseTag + structure;
            caseBlocks.Rule = caseBlock | caseBlocks + caseBlock;
            switchExpr.Rule = assignment | assignment + "!";
            switchStatement.Rule = switchTerm + "(" + switchExpr + ")" + "{" + caseBlocks + "}";
            print.Rule = ifStatement | loopStatement | switchStatement
                | printTerm + "(" + print + ")";
            root.Rule = print;

            Root = root;
        }
    }
}