using System.Xml.Linq;
using log4net;

namespace paperless.Services
{
    public sealed class XmlService : IXmlService
    {
        #region Fields
        private readonly ILog _logger;
        private readonly string _accessLogInputDir = Environment.GetEnvironmentVariable("ACCESSLOG_INPUTDIR") ?? "/app/xml_data/input";
        #endregion

        #region Constructors
        public XmlService()
        {
            _logger = LogManager.GetLogger(typeof(XmlService));
        }
        #endregion

        #region Methods
        // ─────────────────────────────────────────────
        // UPDATE ACCESS LOG
        // ─────────────────────────────────────────────
        public async Task UpdateAsync(Guid id)
        {
            _logger.Info($"Updating access count for DocumentId: '{id}'.");

            try
            {
                // Setup
                var today = DateOnly.FromDateTime(DateTime.Now);
                var fileName = $"accessLog-{today:yyyy-MM-dd}.xml";
                var filePath = Path.Combine(_accessLogInputDir, fileName);

                // Lock file to prevent race conditions
                lock (GetFileLock(filePath))
                {
                    XDocument xdoc;

                    if (File.Exists(filePath))
                    {
                        // Load existing XML
                        xdoc = XDocument.Load(filePath);
                        var root = xdoc.Root ?? throw new InvalidDataException("Missing root element.");

                        // Find existing entry for document with this ID
                        var existingEntry = root.Elements("entry")
                            .FirstOrDefault(e => e.Attribute("documentId")?.Value == id.ToString());
                        if (existingEntry != null)
                        {
                            // Increment access count for existing document
                            var currentCount = int.Parse(existingEntry.Attribute("accessCount")?.Value ?? "0");
                            existingEntry.SetAttributeValue("accessCount", currentCount + 1);
                        }
                        else
                        {
                            // Add new entry
                            root.Add(new XElement("entry",
                                new XAttribute("documentId", id),
                                new XAttribute("accessCount", 1)));
                        }

                    }
                    else
                    {
                        // Create new XML
                        xdoc = new XDocument(
                            new XElement("accessLogs",
                                new XAttribute("batchDate", today.ToString("yyyy-MM-dd")),
                                new XElement("entry",
                                    new XAttribute("documentId", id),
                                    new XAttribute("accessCount", 1))));
                    }

                    // Save XML
                    xdoc.Save(filePath);

                    _logger.Info($"Logged document access for DocumentId: '{id}'.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error logging document access for DocumentId: '{id}', {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────
        private static readonly Dictionary<string, object> _fileLocks = new();
        private static object GetFileLock(string filePath)
        {
            lock (_fileLocks)
            {
                if (!_fileLocks.ContainsKey(filePath))
                {
                    _fileLocks[filePath] = new object();
                }
                return _fileLocks[filePath];
            }
        }
        #endregion
    }
}
