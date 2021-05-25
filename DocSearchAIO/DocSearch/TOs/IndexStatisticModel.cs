namespace DocSearchAIO.DocSearch.TOs
{
    public class IndexStatisticModel
    {
        public string IndexName { get; set; }
        public long DocCount { get; set; }
        public double SizeInBytes { get; set; }
    }
}