using System.Text.Json;
using Trade.Gateway.Api.Contract.Certificate;
using Trade.Gateway.Api.Contract.Events;
using Trade.Gateway.Api.Contract.ReferenceData;

namespace Api.Tests.Events
{
    public class EventEnvelopeExtensionsTests
    {
        [Fact]
        public async Task ConvertChedToEventTest()
        {
            var ched = new DefraUNVTDCHEDProfile()
            {
                SpecifiedConsignment = new Consignment(),
                ExchangedDocument = new ExchangedDocument() { Identifier = "CHEDA.GB.2026.1234567" },
            };

            var @event = ched.ToEventEnvelope("testCorrelationId");

            await Verify(@event);
        }

        [Fact]
        public async Task ConvertIntraToEventTest()
        {
            var intra = new DefraUNVTDINTRAProfile()
            {
                SpecifiedConsignment = new Consignment(),
                ExchangedDocument = new ExchangedDocument() { Identifier = "CHEDA.GB.2026.1234567" },
            };

            var @event = intra.ToEventEnvelope("testCorrelationId");

            await Verify(@event);
        }

        [Fact]
        public async Task ConvertClassificationTreeNodeToEventTest()
        {
            var classificationTreeNode = new DefraUNVTDProfileClassificationTreeNodeDetailResponse
            {
                Source = "traces",
                TreeId = "intra_trade",
                NodePath = "R/N-10000/N-10065/L-10121/L-10301/C-11978",
                NodeId = "R_N-10000_N-10065_L-10121_L-10301_C-11978",
                Node = new DefraUNVTDProfileClassificationTreeNodeDetailResponseNode
                {
                    CnCode = "0101",
                    CertificateModel = new CertificateModelReference
                    {
                        ModelId = 11978,
                        ShortTitle = "11978",
                        LongTitle = "Model animal health certificate",
                        CreatedOn = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero),
                        UpdatedOn = new DateTimeOffset(2024, 1, 2, 10, 0, 0, TimeSpan.Zero),
                    },
                    Selectable = true,
                    NodeType = "certificate_model",
                    Label = "Model animal health certificate",
                },
                Attributes = [],
                ClassificationSectionGroups = [],
                LegislationAttributes = [],
                Taxons = [new Taxon { TaxonId = 101, EppoCode = "EPP001", FaoCode = "FAO001", Name = "Bos taurus", LanguageId = "en" }],
                InvasiveTaxons = [new Taxon { TaxonId = 202, EppoCode = "EPP002", FaoCode = "FAO002", Name = "Invasive species", LanguageId = "en" },],
                RetrievedAt = new DateTimeOffset(2024, 1, 3, 12, 0, 0, TimeSpan.Zero),
            };

            var @event = classificationTreeNode.ToEventEnvelope("testCorrelationId");

            await Verify(@event);
        }
    }
}
