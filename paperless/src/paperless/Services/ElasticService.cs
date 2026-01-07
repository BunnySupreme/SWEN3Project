using AutoMapper;
using Elastic.Clients.Elasticsearch;
using log4net;
using Paperless.Api;
using Paperless.Search.Models;

namespace Paperless.Services
{
    public class ElasticService : IElasticService
    {
        #region Fields
        private readonly ElasticsearchClient _elasticClient;
        private readonly IMapper _mapper;
        private readonly ILog _logger;
        #endregion

        #region Constructors
        public ElasticService(ElasticsearchClient elasticClient, IMapper mapper)
        {
            _elasticClient = elasticClient;
            _mapper = mapper;
            _logger = LogManager.GetLogger(typeof(ElasticService));
        }
        #endregion

        #region Methods
        // ─────────────────────────────────────────────
        // SEARCH
        // ─────────────────────────────────────────────
        public async Task<IReadOnlyList<DocumentReadDto>> SearchAsync(string userId, string searchTerm, CancellationToken ct)
        {
            _logger.Info($"Searching for documents matching searchTerm: {searchTerm}");

            var searchResponse = await _elasticClient.SearchAsync<DocumentSearchModel>(s => s
                    .Indices("documents")
                    .Query(q => q
                        .Bool(b => b
                            .Filter(f => f
                                .Term(t => t
                                    .Field(d => d.UserId)
                                    .Value(userId)
                                )
                            )
                            .Must(m => m
                                .MultiMatch(mm => mm
                                    .Query(searchTerm)
                                    .Fields(
                                        d => d.Title,
                                        d => d.OcrText,
                                        d => d.Summary,
                                        d => d.Tags
                                    )
                                    .Fuzziness(new Fuzziness("Auto"))
                                )
                            )
                        )
                    )
                    .Size(10)
                );


            if (!searchResponse.IsValidResponse)
            {
                _logger.Error($"ElasticSearch search failed. Reason: {searchResponse.ElasticsearchServerError}");
                return Array.Empty<DocumentReadDto>();
            }
            else
            {
                _logger.Info($"ElasticSearch search completed successfully. Found {searchResponse.Hits.Count} documents.");
            }

            return _mapper.Map<IReadOnlyList<DocumentReadDto>>(searchResponse.Documents);
        }

        // ─────────────────────────────────────────────
        // CREATE INDEX
        // ─────────────────────────────────────────────
        public async Task<bool> CreateIndexAsync(DocumentSearchModel document, CancellationToken ct)
        {
            _logger.Info($"Indexing document into Elasticsearch. ID: '{document.Id}'");

            var indexResponse = await _elasticClient.IndexAsync(document, i => i.Index("documents"), ct);
            if (!indexResponse.IsValidResponse)
            {
                _logger.Error($"ElasticSearch indexing failed for Document ID: {document.Id}. Reason: {indexResponse.ElasticsearchServerError}");
            }
            else
            {
                _logger.Info($"Document with ID: {document.Id} indexed successfully in ElasticSearch.");
            }

            return true;
        }

        // ─────────────────────────────────────────────
        // UPDATE INDEX
        // ─────────────────────────────────────────────
        public async Task<bool> UpdateIndexAsync(DocumentSearchModel document, CancellationToken ct)
        {
            _logger.Info($"Updating indexed document in Elasticsearch. ID: '{document.Id}'");

            var indexResponse = await _elasticClient.IndexAsync(document, i => i.Index("documents"), ct);
            if (!indexResponse.IsValidResponse)
            {
                _logger.Error($"ElasticSearch index updating failed for Document ID: {document.Id}. Reason: {indexResponse.ElasticsearchServerError}");
            }
            else
            {
                _logger.Info($"Index for document with ID: {document.Id} updated successfully in ElasticSearch.");
            }

            return true;
        }

        // ─────────────────────────────────────────────
        // DELETE INDEX
        // ─────────────────────────────────────────────
        public async Task<bool> DeleteIndexAsync(Guid id, CancellationToken ct)
        {
            _logger.Info($"Deleting document index in Elasticsearch. ID: '{id}'");

            var indexResponse = await _elasticClient.DeleteAsync("documents", id, ct);
            if (!indexResponse.IsValidResponse)
            {
                _logger.Error($"ElasticSearch index deletion failed for Document ID: {id} - Beware stale search results. Reason: {indexResponse.ElasticsearchServerError}");
            }
            else
            {
                _logger.Info($"Index for document with ID: {id} successfully deleted from ElasticSearch.");
            }

            return true;
        }
        #endregion
    }
}
