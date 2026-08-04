using Trade.Gateway.Api.Contract.Certificate;
using Trade.Gateway.Api.Contract.Events;

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
    }
}
