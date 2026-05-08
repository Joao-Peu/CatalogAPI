using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace CatalogAPI.Infrastructure.Search
{
    public class GameSearchDocument
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; } = string.Empty;

        [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.PtBrMicrosoft)]
        public string Title { get; set; } = string.Empty;

        [SearchableField]
        public string Description { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public string Genre { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public double Price { get; set; }
    }
}
