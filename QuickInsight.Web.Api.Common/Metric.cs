namespace QuickInsight.Web.Api.Common
{
    public class MetricBase<T>
    {
        public string Name { get; set; }
        public T Value { get; set; }
    }

    public class Metric : MetricBase<double>
    {

    }
}
