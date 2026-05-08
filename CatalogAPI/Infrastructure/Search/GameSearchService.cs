using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using CatalogAPI.Domain.Entities;

namespace CatalogAPI.Infrastructure.Search
{
    public class GameSearchService
    {
        private readonly SearchClient _searchClient;
        private const string IndexName = "games-index";

        public GameSearchService(IConfiguration config)
        {
            var endpoint = new Uri(config["AzureSearch:Endpoint"]!);
            var credential = new AzureKeyCredential(config["AzureSearch:ApiKey"]!);
            _searchClient = new SearchClient(endpoint, IndexName, credential);
        }

        // Chamado ao criar/editar jogo — sincronização com SQL
        public async Task IndexGameAsync(Game game)
        {
            var doc = new GameSearchDocument
            {
                Id = game.Id.ToString(),
                Title = game.Title,
                Description = game.Description,
                Genre = game.Genre,
                Price = (double)game.Price
            };
            await _searchClient.MergeOrUploadDocumentsAsync(new[] { doc });
        }

        // Endpoint de busca com Fuzzy Search
        public async Task<IEnumerable<GameSearchDocument>> SearchAsync(string query)
        {
            var options = new SearchOptions
            {
                QueryType = SearchQueryType.Full,   // suporta Lucene/fuzzy
                SearchFields = { "Title", "Description" },
                Select = { "Id", "Title", "Description", "Genre", "Price" },
                IncludeTotalCount = true
            };
            // Fuzzy: adiciona ~ ao final de cada termo
            var fuzzyQuery = string.Join(" ", query.Split(' ').Select(t => $"{t}~"));
            var result = await _searchClient.SearchAsync<GameSearchDocument>(fuzzyQuery, options);
            return result.Value.GetResults().Select(r => r.Document);
        }
    }
}
