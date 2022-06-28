using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Threading;
using System.Linq;
using System.Buffers;
using System;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace server
{
    class OutlineProvider : DocumentSymbolHandlerBase
    {
        private readonly TextDocumentStore store;

        public OutlineProvider(TextDocumentStore store) : base()
        {
            this.store = store;
        }

        public override async Task<SymbolInformationOrDocumentSymbolContainer> Handle(DocumentSymbolParams request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            if (!store.TryGetDocument(request.TextDocument.Uri, out var document)) return null;

            var symbols = document.GetSections()
                .Select(section =>
                {
                    var children = document.GetValues()
                        .Where(z => z.Section == section.Section)
                        .ToArray();
                    return new SymbolInformationOrDocumentSymbol(new DocumentSymbol()
                    {
                        Name = section.Section,
                        Kind = SymbolKind.Namespace,
                        Range = (section.Location.Start, children.OrderByDescending(z => z.ValueLocation.End.Line).ThenBy(z => z.ValueLocation.End.Character).First().ValueLocation.End),
                        SelectionRange = section.Location,
                        Children = document.GetValues()
                                            .Where(z => z.Section == section.Section)
                                            .Select(value => new DocumentSymbol()
                                            {
                                                Name = value.Key,
                                                Kind = SymbolKind.Key,
                                                Range = (value.KeyLocation.Start, value.ValueLocation.End),
                                                SelectionRange = value.KeyLocation,
                                                Children = new Container<DocumentSymbol>(
                                                        new DocumentSymbol()
                                                        {
                                                            Name = value.Value,
                                                            Kind = SymbolKind.Property,
                                                            Range = value.ValueLocation,
                                                            SelectionRange = value.ValueLocation
                                                        }).ToArray()
                                            })
                                            .ToArray()
                    });
                })
                .ToArray();

            return new SymbolInformationOrDocumentSymbolContainer(symbols);
        }

        protected override DocumentSymbolRegistrationOptions CreateRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities)
        {
            return new DocumentSymbolRegistrationOptions()
            {
                DocumentSelector = store.GetRegistrationOptions().DocumentSelector,
                Label = "NIN"
            };
        }
    }
}
