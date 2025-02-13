using Irony.Parsing;

namespace Bish {

    public class BishGrammar : Grammar {

        public BishGrammar() {
            var identifier = new IdentifierTerminal("identifier");
            var numberLiteral = new NumberLiteral("numberLiteral");
            var singleString = new StringLiteral("single_string", "'");
            var doubleString = new StringLiteral("double_string", "\"");
            var tagIdentifier = new IdentifierTerminal("tagIdentifier");

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
            var endTerm = ToTerm("end");
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
            var varTypes = new NonTerminal("varTypes");
            var varNullableTypes = new NonTerminal("varNullableTypes");
            var varModifiedTypes = new NonTerminal("varModifiedTypes");
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
            var print = new NonTerminal("print");
            var root = new NonTerminal("root");

            stringLiteral.Rule = singleString | doubleString;
            boolLiteral.Rule = trueLiteral | falseLiteral;
            literal.Rule = stringLiteral | numberLiteral | boolLiteral | nullLiteral;
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
            varTypes.Rule = intType | numType | stringType | boolType;
            varNullableTypes.Rule = varTypes | varTypes + "?";
            varModifiedTypes.Rule = varNullableTypes | constModifier + varNullableTypes;
            statement.Rule = assignment | varModifiedTypes + identifier
                | varModifiedTypes + identifier + "=" + assignment;
            jumpPos.Rule = endTerm;
            jump.Rule = jumpTerm + jumpPos + "[" + tagIdentifier + "]" | jumpTerm + jumpPos;
            sentence.Rule = Empty | statement | jump;
            sentences.Rule = root | sentence | sentences + ";" + root;
            structure.Rule = "{" + sentences + "}";
            codeBlocks.Rule = sentences | "{" + sentences + "}";
            ifStatement.Rule = codeBlocks | ifTerm + "(" + sentence + ")" + structure
                | ifTerm + "(" + sentence + ")" + structure + elseTerm + structure;
            tag.Rule = tagIdentifier + ":";
            loopStatement.Rule = codeBlocks
                | whileTerm + "(" + sentence + ")" + structure
                | tag + whileTerm + "(" + sentence + ")" + structure
                | doTerm + structure + whileTerm + "(" + sentence + ")"
                | forTerm + "(" + sentence + ";" + sentence + ";" + sentence + ")" + structure;
            print.Rule = ifStatement | loopStatement | printTerm + "(" + print + ")";
            root.Rule = print;

            Root = root;
        }
    }
}