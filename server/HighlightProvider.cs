using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Threading;
using System.Linq;
using System.Buffers;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace server
{
    class HighlightProvider : DocumentHighlightHandlerBase
    {
        private readonly TextDocumentStore store;

        public HighlightProvider(TextDocumentStore store) : base()
        {
            this.store = store;
        }

        public override async Task<DocumentHighlightContainer> Handle(DocumentHighlightParams request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            if (!store.TryGetDocument(request.TextDocument.Uri, out var document)) return null;


            return document.GetValues()
                    .SelectMany(z => new[] {
                        new DocumentHighlight() {
                            Kind = DocumentHighlightKind.Text,
                            Range = z.KeyLocation
                        },
                        new DocumentHighlight() {
                            Kind = DocumentHighlightKind.Text,
                            Range = z.ValueLocation
                        }
                    })
                    .Concat(document.GetSections().Select(z => new DocumentHighlight()
                    {
                        Kind = DocumentHighlightKind.Text,
                        Range = z.Location
                    }))
                .ToArray();
        }

        protected override DocumentHighlightRegistrationOptions CreateRegistrationOptions(DocumentHighlightCapability capability, ClientCapabilities clientCapabilities)
        {
            return new DocumentHighlightRegistrationOptions()
            {
                DocumentSelector = store.GetRegistrationOptions().DocumentSelector,
            };
        }
    }
}
