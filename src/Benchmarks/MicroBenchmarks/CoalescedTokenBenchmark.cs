using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NATS.Client.Internals;

namespace MicroBenchmarks
{
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    [SimpleJob(RuntimeMoniker.Net462)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    public class CoalescedTokenBenchmark
    {
        private static void OnCompleted(string _) { }

        [Benchmark(Baseline = true)]
        public string InFlightRequest()
        {
            var request = new InFlightRequest("a", default, 1000, OnCompleted);
            var id = request.Id;
            request.Dispose();
            return id;
        }

        [Benchmark]
        public string InFlightRequest_Upstream_Coalesced()
        {
            var request = new InFlightRequest_Upstream("a", CoalescedTokens.FromTimeout(1000), 0, OnCompleted);
            var id = request.Id;
            request.Dispose();
            return id;
        }


        [Benchmark]
        public string InFlightRequest_Coalesced()
        {
            var request = new InFlightRequest("a", CoalescedTokens.FromTimeout(1000), default, OnCompleted);
            var id = request.Id;
            request.Dispose();
            return id;
        }
    }
}
