using log4net;
using Paperless.BatchProcessor.DAL;
using Paperless.BatchProcessor.Services.Records;
using Paperless.BatchProcessor.DAL.Repositories;
using System.Xml.Linq;

namespace Paperless.BatchProcessor.Services
{
    public class XmlProcessorService : IXmlProcessorService
    {
        #region Fields
        private readonly IDocumentRepository _documentRepository;
        private readonly DataContext _db;
        private readonly ILog _logger;
        #endregion

        #region Constructors
        public XmlProcessorService(IDocumentRepository documentRepository, DataContext db)
        {
            _documentRepository = documentRepository;
            _db = db;
            _logger = LogManager.GetLogger(typeof(XmlProcessorService));
        }
        #endregion

        #region Methods
        public async Task RunOnceAsync(string inputDir, string archiveDir, string errorDir, string filePattern, CancellationToken ct = default)
        {
            _logger.Info("Looking for files to process.");

            // Check if folders exist, create if not
            Directory.CreateDirectory(inputDir);
            Directory.CreateDirectory(archiveDir);
            Directory.CreateDirectory(errorDir);

            // Retrieve files with specified pattern
            var files = Directory.GetFiles(inputDir, filePattern);

            if (files.Length == 0)
            {
                _logger.Info("No files to process.");
                return;
            }

            _logger.Info($"Found {files.Length} files to process.");

            // Process each file
            foreach (var filePath in files.OrderBy(p => p))
            {
                await ProcessXml(filePath, archiveDir, errorDir);
            }
            return;
        }

        private async Task ProcessXml(string filePath, string archiveDir, string errorDir, CancellationToken ct = default)
        {
            var fileName = Path.GetFileName(filePath);
            _logger.Info($"Processing file: '{fileName}'.");

            try
            {
                // Parse XML
                var (batchDate, entries) = ReadXml(filePath);

                // Update DB with access log
                foreach (AccessEntry entry in entries)
                {
                    // Update DB
                    if(!await _documentRepository.UpdateAccessCountAsync(entry.DocumentId, entry.AccessCount, ct))
                    {
                        _logger.Info($"Could not process access log entry: '{entry}'");
                    }
                }
                await _db.SaveChangesAsync(ct);

                // Archive file
                MoveOrOverwrite(filePath, Path.Combine(archiveDir, fileName));
                _logger.Info($"File '{fileName}' processed and archived to: '{archiveDir}'");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing file '{fileName}': {ex.Message}");

                // Move file to error directory
                MoveOrOverwrite(filePath, Path.Combine(errorDir, fileName));
            }
        }

        // Expected format:
        // <accessLogs batchDate="YYYY-MM-DD">
        //   <entry documentId="GUID-1" accessCount="X" />
        //   <entry documentId="GUID-2" accessCount="Y" />
        // </accessLogs>
        private static (DateOnly batchDate, List<AccessEntry> entries) ReadXml(string filePath)
        {
            // Read XML file
            var xdoc = XDocument.Load(filePath);
            var root = xdoc.Root ?? throw new InvalidDataException("Missing root element.");

            // Read date attribute
            var batchDateAttribute = root.Attribute("batchDate")?.Value ?? throw new InvalidDataException("Missing attribute 'batchDate'.");
            var batchDate = DateOnly.Parse(batchDateAttribute);

            // Read XML elements
            var elements = root.Elements("entry")
                .Select(e =>
                {
                    // Extract attributes from XML
                    var documentIdString = e.Attribute("documentId")?.Value ?? throw new InvalidDataException("Missing 'documentId' attribute.");
                    var accessCountString = e.Attribute("accessCount")?.Value ?? throw new InvalidDataException("Missing 'accessCount' attribute.");

                    // Try to parse extracted attributes
                    if (!Guid.TryParse(documentIdString, out var documentId)) throw new InvalidDataException($"DocumentId not valid Guid: '{documentIdString}'");
                    if (!int.TryParse(accessCountString, out var accessCount)) throw new InvalidDataException($"AccessCount not valid int: '{accessCountString}'");

                    // Validate parsed data
                    if (accessCount < 0) throw new InvalidDataException($"AccessCount must be >= 0, was: '{accessCount}'.");

                    // Return AccessEntry
                    return new AccessEntry(documentId, accessCount);
                })
                .ToList();

            return (batchDate, elements);
        }

        private static void MoveOrOverwrite(string src, string dst)
        {
            // Check if destination directory exists
            var dstDir = Path.GetDirectoryName(dst);
            if (!string.IsNullOrEmpty(dstDir))
            {
                // If no, create
                Directory.CreateDirectory(dstDir);
            }

            // Check if filename exists in destination directory
            if (File.Exists(dst))
            {
                // If yes, delete old file
                File.Delete(dst);
            }

            // Move file from source to destination directory
            File.Move(src, dst);
        }
        #endregion
    }
}