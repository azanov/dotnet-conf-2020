//using System;
//using OmniSharp.Extensions.LanguageServer.Protocol.Document;
//using System.Threading.Tasks;
//using OmniSharp.Extensions.LanguageServer.Protocol.Models;
//using System.Threading;
//using parser;
//using Bogus;
//using System.Security.Cryptography;
//using System.Text;
//using Microsoft.Extensions.Options;
//using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

//namespace server
//{
//    class CompletionProvider : CompletionHandlerBase<_Data>
//    {
//        private readonly TextDocumentStore store;
//        private readonly IOptionsMonitor<NinConfiguration> options;

//        public CompletionProvider(TextDocumentStore store, IOptionsMonitor<NinConfiguration> options) : base()
//        {
//            this.store = store;
//            this.options = options;
//        }

//        protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
//        {
//            return new CompletionRegistrationOptions()
//            {
//                DocumentSelector = store.GetRegistrationOptions().DocumentSelector,
//                ResolveProvider = true,
//                TriggerCharacters = new Container<string>(new[] { "=", "[" }),
//                AllCommitCharacters = new Container<string>(new[] { "\n" })
//            };
//        }

//        protected override async Task<CompletionList<_Data>> HandleParams(CompletionParams request, CancellationToken cancellationToken)
//        {
//            if (options.CurrentValue.Hardrock) return new CompletionList<_Data>();
//            await Task.Yield();
//            if (!store.TryGetDocument(request.TextDocument.Uri, out var document)) return null;
//            var item = document.GetItemAtPosition(request.Position);
//            if (item is null) return new CompletionList<_Data>();

//            using var hasher = MD5.Create();
//            var data = new _Data();
//            var key = new StringBuilder();
//            if (item is NinValue value)
//            {
//                key.Append(value.Section);
//                key.Append(value.Key);
//                data.Section = value.Section;
//                data.Key = value.Key;
//            }

//            var hash = key.Append(request.TextDocument.Uri.ToUnencodedString());
//            var seed = BitConverter.ToInt32(hasher.ComputeHash(Encoding.UTF8.GetBytes(key.ToString())).AsSpan()[0..4]);
//            data.Seed = seed;

//            var faker = new Faker<CompletionItem<_Data>>()
//                .UseSeed(seed)
//                .RuleFor(z => z.Kind, z => z.PickRandom<CompletionItemKind>())
//                .RuleFor(z => z.Deprecated, z => z.Random.Bool(0.2f))
//                .RuleFor(z => z.Label, z => item is NinValue ? z.Commerce.ProductName() : z.Commerce.Department())
//                .RuleFor(z => z.SortText, (_, c) => c.Label)
//                .RuleFor(z => z.FilterText, (_, c) => c.Label)
//                .RuleFor(z => z.InsertText, (_, c) => c.Label)
//                .RuleFor(z => z.Data, (f, c) =>
//                {
//                    return new _Data()
//                    {
//                        Section = data.Section,
//                        Key = data.Key,
//                        Seed = BitConverter.ToInt32(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(c.Label)).AsSpan()[0..4])
//                    };
//                });

//            return new CompletionList<_Data>(faker.Generate(50));

//        }

//        protected override Task<CompletionItem<_Data>> HandleResolve(CompletionItem<_Data> request, CancellationToken cancellationToken)
//        {
//            var faker = new Faker() { Random = new Randomizer(request.Data.Seed) };

            

//            //if (string.IsNullOrEmpty(request.Data.Key))
//            //{
//            //    request.Detail = faker.Commerce.Ean13();
//            //}
//            //else
//            //{
//            //    request.Detail = faker.Commerce.ProductAdjective() + " " + faker.Commerce.ProductMaterial();
//            //}
//            //request.Documentation = faker.Lorem.Paragraphs(2);
//            return Task.FromResult(request);
//        }
//    }
//}
