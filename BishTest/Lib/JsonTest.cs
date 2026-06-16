namespace BishTest.Lib;

public class JsonTest(TestInfoFixture fixture) : LibTest(fixture, "json", ["JSON"])
{
    private void JsonParse(string text, string? result = null)
    {
        Scope.DefVar("text", new BishString(text));
        ExpectResult("JSON.parse(text)", result ?? text);
    }

    private void JsonParseError(string text)
    {
        Scope.DefVar("text", new BishString(text));
        ExpectErrorResult("try JSON.parse(text)");
    }

    [Fact]
    public void TestJsonParse()
    {
        JsonParse("true");
        JsonParse("false");
        JsonParse("\nnull\t");
        JsonParseError("true false");

        JsonParse("0");
        JsonParse("-0");
        JsonParse("-0.0");
        JsonParse("1");
        JsonParse("-1");
        JsonParse("1.5");
        JsonParse("-1.5");
        JsonParse("3.1416");
        JsonParse("1E10", "1.0e10");
        JsonParse("1e10", "1.0e10");
        JsonParse("1E+10", "1.0e10");
        JsonParse("1E-10", "1.0e-10");
        JsonParse("-1E10", "-1.0e10");
        JsonParse("-1e10", "-1.0e10");
        JsonParse("-1E+10", "-1.0e10");
        JsonParse("-1E-10", "-1.0e-10");
        JsonParse("1.234E+10", "1.234e+10");
        JsonParse("1.234E-10", "1.234e-10");
        JsonParseError("+0");
        JsonParseError("+1");
        JsonParseError(".123");
        JsonParseError("1.");
        JsonParseError("INF");
        JsonParseError("Inf");
        JsonParseError("Infinity");
        JsonParseError("NaN");

        JsonParse(""" "" """);
        JsonParse(""" "Hello" """);
        JsonParse(""" "你好" """);
        JsonParse(""" " " """);
        JsonParse(""" "\/" """);
        JsonParse(""" "\\" """);
        JsonParse(""" "\"" """);
        JsonParse(""" "\b" """);
        JsonParse(""" "\f" """);
        JsonParse(""" "\n" """);
        JsonParse(""" "\r" """);
        JsonParse(""" "\t" """);
        JsonParse(""" "\u0041" """);
        JsonParse(""" "\u597d" """);
        JsonParse(""" "\u0022" """);
        JsonParse(""" "\uD83D\uDE00" """);
        JsonParseError(""" "Unterminated """);
        JsonParseError("""
                        "Line
                       Break" 
                       """);
        JsonParseError(""" "\z" """);
        JsonParseError(""" "\u123" """);
        JsonParseError(""" "\u123G" """);

        JsonParse("[]");
        JsonParse("[true]");
        JsonParse("[1, 2, 3]");
        JsonParse("[\"a\", \"b\"]");
        JsonParse("[ ]");
        JsonParse("[\n\t1,\n2\r]");
        JsonParse("[ 1 , 2 ]");
        JsonParse("[true, 1, \"hi\", null]");
        JsonParse("[[]]");
        JsonParse("[[1, 2], [3, 4]]");
        JsonParse("[1, [2, [3]]]");
        JsonParseError("[");
        JsonParseError("[1,2");
        JsonParseError("[1,]");
        JsonParseError("[1,,2]");
        JsonParseError("[,1]");
        JsonParseError("[1 2]");

        JsonParse("{}");
        JsonParse("""{"a": 1}""");
        JsonParse("""{"a": 1, "b": 2}""");
        JsonParse("""{"": 0}""");
        JsonParse("""{"outer": {"inner": true}}""");
        JsonParse("""{"list": [1, 2, {"x": 0}]}""");
        JsonParse("""{"a": {"b": {"c": {}}}}""");
        JsonParse("""{ "key" : "value" }""");
        JsonParse("""
                  {
                    "a": 1,
                    "b": 2
                  }
                  """);
        JsonParse("""{"k":"v"}""");
        JsonParseError("{a: 1}");
        JsonParseError("{'a': 1}");
        JsonParseError("""{"a": 1,}""");
        JsonParseError("""{"a": 1 "b": 2}""");
        JsonParseError("""{"a" 1}""");
        JsonParseError("""{"a": 1, , "b": 2}""");
    }

    private void JsonStringify(string expr, string? result = null, int? tabs = null)
    {
        Scope.DefVar("result", new BishString((result ?? expr).Replace("\r\n", "\n")));
        ExpectResult($"JSON.stringify({expr}{(tabs is null ? "" : $",{tabs}")})", "result");
        ExpectResult($"'{{:{(tabs is null ? "" : $".{tabs}")}j}}'.format({expr})", "result");
    }

    private void JsonStringifyError(string expr)
    {
        ExpectErrorResult($"try JSON.stringify({expr})");
        ExpectErrorResult($"try '{{:j}}'.format({expr})");
    }

    [Fact]
    public void TestJsonStringify()
    {
        JsonStringify("true");
        JsonStringify("false");
        JsonStringify("null");
        JsonStringify("123.4");
        JsonStringify("""{"a":1,"b":"2"}""");
        JsonStringify("""[1,"test",[3,4]]""");
        JsonStringify("""{"users":[{"id":1},{"id":2}],"active":true}""");
        JsonStringify("{.toJSON:func(){'a':0}}", """{"a":0}""");
        JsonStringify("""
                      {
                        "users": [
                          {
                            "id": 1
                          },
                          {
                            "id": 2
                          }
                        ],
                        "active": true
                      }
                      """, tabs: 2);
        JsonStringifyError("{0:0}");
        JsonStringifyError("object()");
    }

    [Fact]
    public void TestJsonExtensions()
    {
        JsonStringify("meta.parse('1+2')",
            """
                {"type":"Program","children":[{"type":"BinOpExpr","children":
                [{"type":"AtomExpr","children":[{"type":"IntAtom","children":
                [{"type":"TerminalNodeImpl","text":"1"}]}]},{"type":"TerminalNodeImpl","text":"+"},
                {"type":"AtomExpr","children":[{"type":"IntAtom","children":
                [{"type":"TerminalNodeImpl","text":"2"}]}]}]},
                {"type":"TerminalNodeImpl","text":"<EOF>"}]}
                """.Replace(Environment.NewLine, ""));
    }
}