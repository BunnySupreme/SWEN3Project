using Elastic.Clients.Elasticsearch;
using log4net;
using Minio.DataModel.Notification;
using Paperless.Search.Models;

namespace Paperless.Services
{
    public class ElasticService : IElasticService
    {
        #region Fields
        private readonly ElasticsearchClient _elasticClient;
        private readonly ILog _logger;
        #endregion

        #region Constructors
        public ElasticService(ElasticsearchClient elasticClient)
        {
            _elasticClient = elasticClient;
            _logger = LogManager.GetLogger(typeof(ElasticService));
        }
        #endregion

        #region Methods
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
