using Irony.Parsing;

namespace Bish {

    public class BishGrammar : Grammar {

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

            var stringLiteral = new NonTerminal("stringLiteral");
            var boolLiteral = new NonTerminal("boolLiteral");
            var literal = new NonTerminal("literal");
            var factor = new NonTerminal("factor");
            var powerExpr = new NonTerminal("powerExpr");
            var term = new NonTerminal("term");
            var expression = new NonTerminal("expression");
            var assignment = new NonTerminal("assignment");
            var varTypes = new NonTerminal("varTypes");
            var varNullableTypes = new NonTerminal("varNullableTypes");
            var statement = new NonTerminal("statement");
            var sentence = new NonTerminal("sentence");
            var sentences = new NonTerminal("sentences");
            var codeBlocks = new NonTerminal("codeBlocks");

            stringLiteral.Rule = singleString | doubleString;
            boolLiteral.Rule = trueLiteral | falseLiteral;
            literal.Rule = stringLiteral | numberLiteral | boolLiteral | nullLiteral;
            factor.Rule = "!" + factor
                | "+" + factor | "-" + factor | factor + "++" | factor + "--"
                | literal | identifier | "(" + expression + ")";
            powerExpr.Rule = factor | powerExpr + "^" + factor;
            term.Rule = powerExpr | term + "*" + powerExpr | term + "/" + powerExpr;
            expression.Rule = term | expression + "+" + term | expression + "-" + term;
            assignment.Rule = expression | identifier + "=" + assignment;
            varTypes.Rule = intType | numType | stringType | boolType;
            varNullableTypes.Rule = varTypes | varTypes + "?";
            statement.Rule = assignment | varNullableTypes + identifier
                | varNullableTypes + identifier + "=" + assignment;
            sentence.Rule = statement | Empty;
            sentences.Rule = sentence | sentences + ";" + sentence;
            codeBlocks.Rule = sentences | "{" + sentences + "}";

            Root = codeBlocks;
        }
    }
}