using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol;
using MediatR;
using System.Threading;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System.Linq;
using System.Collections.Immutable;
using parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System.Buffers;

namespace server
{
    public class TextDocumentStore : ITextDocumentSyncHandler
    {
        private readonly TextDocumentCloseRegistrationOptions _closeOptions;
        private readonly TextDocumentOpenRegistrationOptions _openOptions;
        private readonly TextDocumentChangeRegistrationOptions _changeOptions;
        private readonly TextDocumentSaveRegistrationOptions _saveOptions;
        private readonly ILanguageServerFacade languageServer;
        private SynchronizationCapability _capability;
        private ImmutableDictionary<DocumentUri, NinDocument> _openDocuments = ImmutableDictionary<DocumentUri, NinDocument>.Empty.WithComparers(DocumentUri.Comparer);
        private ImmutableDictionary<DocumentUri, SemanticTokensDocument> _tokenDocuments = ImmutableDictionary<DocumentUri, SemanticTokensDocument>.Empty.WithComparers(DocumentUri.Comparer);

        public TextDocumentStore(ILanguageServerFacade languageServer)
        {
            var selector = new DocumentSelector(DocumentSelector.ForPattern("**/*.ini", "**/*.nin").Concat(DocumentSelector.ForScheme("ini")));
            _changeOptions = new TextDocumentChangeRegistrationOptions()
            {
                DocumentSelector = selector,
                SyncKind = TextDocumentSyncKind.Incremental,
            };
            _openOptions = new TextDocumentOpenRegistrationOptions()
            {
                DocumentSelector = selector
            };
            _closeOptions = new TextDocumentCloseRegistrationOptions()
            {
                DocumentSelector = selector
            };
            _saveOptions = new TextDocumentSaveRegistrationOptions()
            {
                DocumentSelector = selector,
                IncludeText = true,
                
            };
            this.languageServer = languageServer;
        }

        public NinDocument? GetDocument(DocumentUri documentUri)
        {
            return _openDocuments.TryGetValue(documentUri, out var value) ? value : null;
        }

        public bool TryGetDocument(DocumentUri documentUri, out NinDocument document)
        {
            return _openDocuments.TryGetValue(documentUri, out document);
        }

        public bool TryGetTokenDocument(DocumentUri documentUri, SemanticTokensRegistrationOptions options, out SemanticTokensDocument document)
        {
            if (_openDocuments.TryGetValue(documentUri, out _))
            {
                if (!_tokenDocuments.TryGetValue(documentUri, out document))
                    _tokenDocuments = _tokenDocuments.Add(documentUri, document = new SemanticTokensDocument(options));
                return true;
            }
            document = null;
            return false;
        }

        public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            if (uri.Path.EndsWith(".ini"))
            {
                return new TextDocumentAttributes(uri, "ini");
            }
            if (uri.Path.EndsWith(".nin"))
            {
                return new TextDocumentAttributes(uri, "ini");
            }
            return null;
        }

        public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            if (!_openDocuments.TryGetValue(request.TextDocument.Uri, out var value)) return Unit.Task;
            var changes = request.ContentChanges.ToArray();
            // full text change;
            if (changes.Length == 1 && changes[0].Range == default)
            {
                value.Load(changes[0].Text);
            }
            else
            {
                value.Update(changes);
            }

            languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
            {
                Diagnostics = new Container<Diagnostic>(value.GetDiagnostics()),
                Uri = value.DocumentUri,
                Version = value.Version
            });
            return Unit.Task;
        }

        public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            lock (_openDocuments)
            {
                var document = new NinDocument(request.TextDocument.Uri);
                _openDocuments = _openDocuments.Add(request.TextDocument.Uri, document);
                document.Load(request.TextDocument.Text);

                languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
                {
                    Diagnostics = new Container<Diagnostic>(document.GetDiagnostics()),
                    Uri = document.DocumentUri,
                    Version = document.Version
                });
            }

            return Unit.Task;
        }

        public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            lock (_openDocuments)
            {
                _openDocuments = _openDocuments.Remove(request.TextDocument.Uri);
            }

            return Unit.Task;
        }

        public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            if (!_capability.DidSave) return Unit.Task;
            if (_openDocuments.TryGetValue(request.TextDocument.Uri, out var value))
            {
                value.Load(request.Text);

                languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
                {
                    Diagnostics = new Container<Diagnostic>(value.GetDiagnostics()),
                    Uri = value.DocumentUri,
                    Version = value.Version
                });
            }

            return Unit.Task;
        }

        public void SetCapability(SynchronizationCapability capability) { _capability = capability; }
        public TextDocumentChangeRegistrationOptions GetRegistrationOptions() { return _changeOptions; }

        
        public TextDocumentChangeRegistrationOptions GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return _changeOptions;
        }

        TextDocumentOpenRegistrationOptions IRegistration<TextDocumentOpenRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return _openOptions;
        }

        TextDocumentCloseRegistrationOptions IRegistration<TextDocumentCloseRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return _closeOptions;
        }

        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return _saveOptions;
        }
    }
}
