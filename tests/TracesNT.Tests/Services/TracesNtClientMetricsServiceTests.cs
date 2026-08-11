using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using TracesNT.Services;

namespace TracesNT.Tests.Services;

public class TracesNtClientMetricsServiceTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMeterFactory _meterFactory;

    public TracesNtClientMetricsServiceTests()
    {
        _serviceProvider = CreateServiceProvider();
        _meterFactory = _serviceProvider.GetRequiredService<IMeterFactory>();
    }
    
    private static ServiceProvider CreateServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMetrics();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<ITracesNtClientMetricsService, TracesNtClientMetricsService>();
        return serviceCollection.BuildServiceProvider();
    }

    private MetricCollector<T> GetCollector<T>(string instrumentName)
        where T : struct
    {
        return new MetricCollector<T>(_meterFactory, TracesNtClientMetricsService.MeterName, instrumentName);
    }
    
    [Fact]
    public void RecordRequest_ShouldEmitMeasurement()
    {
        var metricsService = _serviceProvider.GetRequiredService<ITracesNtClientMetricsService>();
        var requestDurationCollector = GetCollector<long>("RequestDuration");

        metricsService.RecordRequest("some-tracesnt-action", 123, 200, null);
        metricsService.RecordRequest("some-tracesnt-action", 456, 500, "Client");
        
        var receivedMeasurements = requestDurationCollector.GetMeasurementSnapshot();
        receivedMeasurements.Count.Should().Be(2);
        receivedMeasurements[0].Value.Should().Be(123);
        receivedMeasurements[0].ContainsTags("Action").Should().BeTrue();
        receivedMeasurements[0].Tags["Action"].Should().Be("some-tracesnt-action");
        receivedMeasurements[0].ContainsTags("ResponseStatusCode").Should().BeTrue();
        receivedMeasurements[0].Tags["ResponseStatusCode"].Should().Be(200);
        receivedMeasurements[0].ContainsTags("FaultCode").Should().BeTrue();
        receivedMeasurements[0].Tags["FaultCode"].Should().BeNull();
        
        receivedMeasurements[1].Value.Should().Be(456);
        receivedMeasurements[1].ContainsTags("Action").Should().BeTrue();
        receivedMeasurements[1].Tags["Action"].Should().Be("some-tracesnt-action");
        receivedMeasurements[1].ContainsTags("ResponseStatusCode").Should().BeTrue();
        receivedMeasurements[1].Tags["ResponseStatusCode"].Should().Be(500);
        receivedMeasurements[1].ContainsTags("FaultCode").Should().BeTrue();
        receivedMeasurements[1].Tags["FaultCode"].Should().Be("Client");
    }
}