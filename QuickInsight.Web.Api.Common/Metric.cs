using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuickInsight.Web.Api.Common
{

    public partial class MetricAggregator
    {
        private SpinLock _trackLock = new SpinLock();

        public DateTimeOffset StartTimestamp { get; }
        public int Count { get; private set; }
        public double Sum { get; private set; }
        public double SumOfSquares { get; private set; }
        public double Min { get; private set; }
        public double Max { get; private set; }

        public double Average => Count == 0 ? 0 : Sum / Count;

        public double Variance => (Count == 0)
            ? 0
            : SumOfSquares / Count
              - Average * Average;

        public double StandardDeviation => Math.Sqrt(Variance);

        public MetricAggregator(DateTimeOffset startTimestamp)
        {
            StartTimestamp = startTimestamp;
        }

        public void TrackValue(double value)
        {
            bool lockAcquired = false;

            try
            {
                _trackLock.Enter(ref lockAcquired);

                if ((Count == 0) || (value < Min))
                {
                    Min = value;
                }

                if ((Count == 0) || (value > Max))
                {
                    Max = value;
                }

                Count++;
                Sum += value;
                SumOfSquares += value * value;
            }
            finally
            {
                if (lockAcquired)
                {
                    _trackLock.Exit();
                }
            }
        }
    }


    public sealed class Metric : IDisposable
    {
        private static readonly TimeSpan AggregationPeriod = TimeSpan.FromSeconds(60);

        private bool _isDisposed;
        private MetricAggregator _aggregator;
        //private readonly TelemetryClient _telemetryClient;

        public string Name { get; }

        public Metric(string name /*TelemetryClient telemetryClient*/)
        {
            Name = name ?? "null";
            _aggregator = new MetricAggregator(DateTimeOffset.UtcNow);
            //this._telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));

            Task.Run(AggregatorLoopAsync);
        }

        public void TrackValue(double value)
        {
            var currAggregator = _aggregator;
            if (currAggregator != null)
            {
                currAggregator.TrackValue(value);
            }
        }

        private async Task AggregatorLoopAsync()
        {
            //while (_isDisposed == false)
            //{
            //    try
            //    {
            //        await Task.Delay(AggregationPeriod).ConfigureAwait(false);

            //        var nextAggregator = new MetricAggregator(DateTimeOffset.UtcNow);
            //        var prevAggregator = Interlocked.Exchange(ref _aggregator, nextAggregator);

            //        if (prevAggregator != null && prevAggregator.Count > 0)
            //        {
            //            var aggPeriod = nextAggregator.StartTimestamp - prevAggregator.StartTimestamp;
            //            if (aggPeriod.TotalMilliseconds < 1)
            //            {
            //                aggPeriod = TimeSpan.FromMilliseconds(1);
            //            }

            //            //var aggregatedMetricTelemetry = new MetricTelemetry(
            //            //    Name,
            //            //    prevAggregator.Count,
            //            //    prevAggregator.Sum,
            //            //    prevAggregator.Min,
            //            //    prevAggregator.Max,
            //            //    prevAggregator.StandardDeviation);
            //            //aggregatedMetricTelemetry.Properties["AggregationPeriod"] = aggPeriod.ToString("c");

            //            //_telemetryClient.Track(aggregatedMetricTelemetry);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        throw;
            //    }
            //}
        }

        void IDisposable.Dispose()
        {
            _isDisposed = true;
            _aggregator = null;
        }
    }


    public partial class MetricAggregator
    {
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
    }

    public class MetricAggregatorEqualityComparer : IEqualityComparer<MetricAggregator>
    {
        public int GetHashCode(MetricAggregator metricAggregator) { return metricAggregator.Properties.Select(kvp => kvp.Key + "=" + kvp.Value).GetOrderIndependentHashCode(); }
        public bool Equals(MetricAggregator rhs, MetricAggregator lhs) { return rhs.Properties.DictionaryEqual(lhs.Properties); }
    }

    public static class HashExtensions
    {
        public static int GetOrderIndependentHashCode<T>(this IEnumerable<T> source)
        {
            return source.Aggregate(0, (current, element) => unchecked(current + EqualityComparer<T>.Default.GetHashCode(element)));
        }
    }

    public static class DictionaryHelper
    {
        public static bool DictionaryEqual<TKey, TValue>(
            this IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
        {
            return first.DictionaryEqual(second, null);
        }

        public static bool DictionaryEqual<TKey, TValue>(
            this IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second,
            IEqualityComparer<TValue> valueComparer)
        {
            if (first == second) return true;
            if ((first == null) || (second == null)) return false;
            if (first.Count != second.Count) return false;

            valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;

            foreach (var kvp in first)
            {
                if (!second.TryGetValue(kvp.Key, out var secondValue)) return false;
                if (!valueComparer.Equals(kvp.Value, secondValue)) return false;
            }
            return true;
        }
    }
}
