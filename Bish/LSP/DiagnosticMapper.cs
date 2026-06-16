using BishRuntime;

namespace Bish.LSP;

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

public static class DiagnosticMapper
{
    public static Diagnostic ToLspDiagnostic(CompilationError error) =>
        new()
        {
            Range = new Range(
                new Position(error.Position.Line - 1, error.Position.Column),
                new Position(error.Position.StopLine - 1, error.Position.StopColumn)
            ),
            Message = error.Message,
            Severity = DiagnosticSeverity.Error,
            Source = "BishParser"
        };
}