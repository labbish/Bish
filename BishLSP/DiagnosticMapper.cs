using BishCompiler;

namespace BishLSP;

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

public static class DiagnosticMapper
{
    public static Diagnostic ToLspDiagnostic(CompilationError error) =>
        new()
        {
            Range = new Range(
                new Position(error.Line - 1, error.Column),
                new Position(error.StopLine - 1, error.StopColumn)
            ),
            Message = error.Message,
            Severity = DiagnosticSeverity.Error,
            Source = "BishParser"
        };
}