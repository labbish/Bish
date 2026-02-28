using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace BishLSP;

public class TextDocumentHandler(ILanguageServerFacade facade) : TextDocumentSyncHandlerBase
{
    private ILanguageServerFacade Facade => facade;

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new(uri, "bish");

    private Task ParseAndPublishDiagnosticsAsync(string text, DocumentUri uri)
    {
        BishCompiler.BishCompiler.Compile(text, out var errors, optimize: false, runs: false);

        var diagnostics = errors.Select(DiagnosticMapper.ToLspDiagnostic).ToList();

        Facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = uri,
            Diagnostics = Container<Diagnostic>.From(diagnostics)
        });

        return Task.CompletedTask;
    }

    public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        await ParseAndPublishDiagnosticsAsync(request.TextDocument.Text, request.TextDocument.Uri);
        return Unit.Value;
    }

    public override async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var text = request.ContentChanges.First().Text;
        await ParseAndPublishDiagnosticsAsync(text, request.TextDocument.Uri);
        return Unit.Value;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken) => Unit.Task;
    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken) => Unit.Task;

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        TextSynchronizationCapability capability, 
        ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("bish"),
            Change = TextDocumentSyncKind.Full
        };
}