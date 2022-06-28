using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Threading;
using System.Linq;
using parser;
using System.Collections.Generic;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace server
{
    class SelectionRangeProvider : SelectionRangeHandlerBase
    {

        private readonly TextDocumentStore store;

        public SelectionRangeProvider(TextDocumentStore store) : base()
        {
            this.store = store;
        }

        public override async Task<Container<SelectionRange>> Handle(SelectionRangeParams request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            if (!store.TryGetDocument(request.TextDocument.Uri, out var document)) return null;

            var results = new List<SelectionRange>();
            foreach (var position in request.Positions)
            {

                var range = document.GetItemAtPosition(position) switch
                {
                    NinValue v => GetSelectionRangeForValue(position, v, document),
                    NinSection s => new SelectionRange()
                    {
                        Range = s.Location,
                        Parent = new SelectionRange() { Range = ((s.Location.Start.Line, s.Location.Start.Character - 1), (s.Location.End.Line, s.Location.End.Character + 1)) }
                    },
                    _ => null
                };
                if (range == null) continue;
                results.Add(range);
            }

            return results;

            static SelectionRange GetSelectionRangeForValue(Position position, NinValue value, NinDocument document)
            {
                var ranges = new List<Range>();
                if (position >= value.ValueLocation.Start && position <= value.ValueLocation.End)
                {
                    ranges.Add(value.ValueLocation);
                }
                if (position >= value.KeyLocation.Start && position <= value.KeyLocation.End)
                {
                    ranges.Add(value.KeyLocation);
                }
                ranges.Add((value.KeyLocation.Start, value.ValueLocation.End));

                var section = document.GetSections().Single(z => z.Section == value.Section);

                var end = Enumerable.MaxBy(document.GetValues()
                    .Where(x => x.Section == value.Section),
                    z => z.ValueLocation.End);
                if (end != null)
                {
                    ranges.Add(((section.Location.Start.Line, section.Location.Start.Character - 1), end.ValueLocation.End));
                }

                ranges.Reverse();
                var result = ranges.Aggregate<Range, SelectionRange>(null, (acc, value) => new SelectionRange()
                {
                    Range = value,
                    Parent = acc
                });
                return result;
            }
        }

        protected override SelectionRangeRegistrationOptions CreateRegistrationOptions(SelectionRangeCapability capability, ClientCapabilities clientCapabilities)
        {
            return new SelectionRangeRegistrationOptions()
            {
                DocumentSelector = store.GetRegistrationOptions().DocumentSelector
            };
        }
    }
}
