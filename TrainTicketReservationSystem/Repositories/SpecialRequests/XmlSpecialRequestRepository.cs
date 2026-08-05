using System.Globalization;
using System.Xml.Linq;
using TrainTicketReservationSystem.Models.SpecialRequests;

namespace TrainTicketReservationSystem.Repositories.SpecialRequests
{
  public class XmlSpecialRequestRepository : ISpecialRequestRepository
  {
     /*
      * Only one request may read or write the XML file at a time.
      * This prevents two simultaneous operations from corrupting
      * or overwriting the file.
      */
    private static readonly SemaphoreSlim FileLock = new(1, 1);
    private readonly string _filePath;
    private readonly ILogger<XmlSpecialRequestRepository> _logger;

    public XmlSpecialRequestRepository(IConfiguration configuration, IWebHostEnvironment environment, ILogger<XmlSpecialRequestRepository> logger)
    {
      _logger = logger;
      var configuredPath = configuration["SpecialRequests:FilePath"];

      // Local default: <project folder>/App_Data/special-requests.xml
      if (string.IsNullOrWhiteSpace(configuredPath))
      {
        _filePath = Path.Combine(environment.ContentRootPath, "App_Data", "special-requests.xml");
      }
      else if (Path.IsPathRooted(configuredPath))
      {
        _filePath = configuredPath;
      }
      else
      {
        _filePath = Path.Combine(environment.ContentRootPath, configuredPath);
      }
    }

    public async Task<IReadOnlyList<SpecialRequestRecord>>GetAllRequests(CancellationToken cancellationToken = default)
    {
      await FileLock.WaitAsync(cancellationToken);

      try
      {
        var document = await LoadOrCreateDocument(cancellationToken);

        return document.Root!
            .Elements("SpecialRequest")
            .Select(ToRecord)
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToList();
      }
      finally
      {
        FileLock.Release();
      }
    }

    public async Task<SpecialRequestRecord?> GetRequestById(Guid specialRequestId, CancellationToken cancellationToken = default)
    {
      await FileLock.WaitAsync(cancellationToken);

      try
      {
        var document = await LoadOrCreateDocument(cancellationToken);

        var element = document.Root!
            .Elements("SpecialRequest")
            .FirstOrDefault(item => GetGuidValue(item, "SpecialRequestId") == specialRequestId);

        return element is null
            ? null
            : ToRecord(element);
      }
      finally
      {
        FileLock.Release();
      }
    }

    public async Task<IReadOnlyList<SpecialRequestRecord>>GetRequestByBookingId(Guid bookingId, CancellationToken cancellationToken = default)
    {
      await FileLock.WaitAsync(cancellationToken);

      try
      {
        var document = await LoadOrCreateDocument(cancellationToken);

        return document.Root!
            .Elements("SpecialRequest")
            .Where(item => GetGuidValue(item, "BookingId") == bookingId)
            .Select(ToRecord)
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToList();
      }
      finally
      {
        FileLock.Release();
      }
    }

    public async Task AddRequest(SpecialRequestRecord specialRequest, CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(specialRequest);
      await FileLock.WaitAsync(cancellationToken);

      try
      {
        var document = await LoadOrCreateDocument(cancellationToken);
        var alreadyExists = document.Root!
            .Elements("SpecialRequest")
            .Any(item => GetGuidValue(item, "SpecialRequestId") == specialRequest.SpecialRequestId);

        if (alreadyExists)
        {
          throw new InvalidOperationException("A special request with the same ID already exists.");
        }

        document.Root!.Add(ToXmlElement(specialRequest));
        await SaveDocument(document, cancellationToken);
        _logger.LogInformation("Special request {SpecialRequestId} was saved to XML.", specialRequest.SpecialRequestId);
      }
      finally
      {
        FileLock.Release();
      }
    }

    public async Task<bool> UpdateRequest(SpecialRequestRecord specialRequest, CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(specialRequest);
      await FileLock.WaitAsync(cancellationToken);

      try
      {
        var document = await LoadOrCreateDocument(cancellationToken);
        var existingElement = document.Root!
            .Elements("SpecialRequest")
            .FirstOrDefault(item => GetGuidValue(item, "SpecialRequestId") == specialRequest.SpecialRequestId);

        if (existingElement is null)
        {
          return false;
        }

        //Replace the old XML element with a new element containing the updated information.
        existingElement.ReplaceWith(ToXmlElement(specialRequest));
        await SaveDocument(document, cancellationToken);
        _logger.LogInformation("Special request {SpecialRequestId} was updated in XML.", specialRequest.SpecialRequestId);

        return true;
      }
      finally
      {
        FileLock.Release();
      }
    }

    public async Task<bool> DeleteRequest(Guid specialRequestId, CancellationToken cancellationToken = default)
    {
      await FileLock.WaitAsync(cancellationToken);

      try
      {
        var document = await LoadOrCreateDocument(cancellationToken);
        var existingElement = document.Root!
            .Elements("SpecialRequest")
            .FirstOrDefault(item => GetGuidValue(item, "SpecialRequestId") == specialRequestId);

        if (existingElement is null)
        {
          return false;
        }

        existingElement.Remove();
        await SaveDocument(document, cancellationToken);
        _logger.LogInformation("Special request {SpecialRequestId} was deleted from XML.", specialRequestId);

        return true;
      }
      finally
      {
        FileLock.Release();
      }
    }

    private async Task<XDocument> LoadOrCreateDocument(CancellationToken cancellationToken)
    {
      // When the XML file does not exist, create a new in-memory XML document with one root element.
      if (!File.Exists(_filePath))
      {
        return new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("SpecialRequests"));
      }

      await using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, options: FileOptions.Asynchronous);
      var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

      if (document.Root?.Name != "SpecialRequests")
      {
        throw new InvalidDataException("The XML file must contain a SpecialRequests root element.");
      }

      return document;
    }

    private async Task SaveDocument(XDocument document, CancellationToken cancellationToken)
    {
      var directory = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrWhiteSpace(directory))
      {
        Directory.CreateDirectory(directory);
      }

      // Write to a temporary file first. Only replace the main file after the save succeeds.
      var temporaryFilePath = _filePath + ".tmp";

      try
      {
        await using (var stream = new FileStream(temporaryFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, options: FileOptions.Asynchronous))
        {
          await document.SaveAsync(stream, SaveOptions.None, cancellationToken);
          await stream.FlushAsync(cancellationToken);
        }

        File.Move(temporaryFilePath, _filePath, overwrite: true);
      }
      catch
      {
        if (File.Exists(temporaryFilePath))
        {
          File.Delete(temporaryFilePath);
        }

        throw;
      }
    }

    private static XElement ToXmlElement(SpecialRequestRecord specialRequest)
    {
      return new XElement("SpecialRequest",
          new XElement("SpecialRequestId", specialRequest.SpecialRequestId),
          new XElement("BookingId", specialRequest.BookingId),
          new XElement("RequestType", specialRequest.RequestType),
          new XElement("Description", specialRequest.Description),
          new XElement("Status", specialRequest.Status),
          new XElement("CreatedAtUtc", specialRequest.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture)),
          new XElement("UpdatedAtUtc", specialRequest.UpdatedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty));
    }

    private static SpecialRequestRecord ToRecord(XElement element)
    {
      return new SpecialRequestRecord
      {
        SpecialRequestId = GetGuidValue(element, "SpecialRequestId"),
        BookingId = GetGuidValue(element, "BookingId"),
        RequestType = GetRequiredValue(element, "RequestType"),
        Description = GetRequiredValue(element, "Description"),
        Status = GetRequiredValue(element, "Status"),
        CreatedAtUtc = GetDateTimeOffsetValue(element, "CreatedAtUtc"),
        UpdatedAtUtc = GetNullableDateTimeOffsetValue(element, "UpdatedAtUtc")
      };
    }

    private static string GetRequiredValue(XElement parent, string elementName)
    {
      var value = parent.Element(elementName)?.Value;

      if (string.IsNullOrWhiteSpace(value))
      {
        throw new InvalidDataException($"The XML element '{elementName}' is missing or empty.");
      }

      return value;
    }

    private static Guid GetGuidValue(XElement parent, string elementName)
    {
      var value = parent.Element(elementName)?.Value;

      if (!Guid.TryParse(value, out var result))
      {
        throw new InvalidDataException($"The XML element '{elementName}' does not contain a valid GUID.");
      }

      return result;
    }

    private static DateTimeOffset GetDateTimeOffsetValue(XElement parent, string elementName)
    {
      var value = parent.Element(elementName)?.Value;

      if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
      {
        throw new InvalidDataException($"The XML element '{elementName}' does not contain a valid date.");
      }

      return result;
    }

    private static DateTimeOffset? GetNullableDateTimeOffsetValue(XElement parent, string elementName)
    {
      var value = parent.Element(elementName)?.Value;

      if (string.IsNullOrWhiteSpace(value))
      {
        return null;
      }

      if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
      {
        throw new InvalidDataException($"The XML element '{elementName}' does not contain a valid date.");
      }

      return result;
    }
  }
}
