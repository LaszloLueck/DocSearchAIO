namespace DocSearchAIO.DocSearch.TOs
{
    public class IndexStatisticModel
    {
        public string IndexName { get; set; }
        public long DocCount { get; set; }
        public double SizeInBytes { get; set; }
        
        public long FetchTimeMs { get; set; }
        
        public long FetchTotal { get; set; }
        public long QueryTimeMs { get; set; }
        public long QueryTotal { get; set; }
        public long SuggestTimeMs { get; set; }
        public long SuggestTotal { get; set; }
    }
}