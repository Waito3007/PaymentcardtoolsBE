using System;
using Paymentcardtools.Models.Enum;

namespace Paymentcardtools.Models.DataModel;

public class BatchKeyRequest
{
    public List<string> Keys { get; set; } = new();
    public CryptoAlgorithm Algorithm { get; set; } = CryptoAlgorithm.TripleDes;
}

public class FileUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public CryptoAlgorithm Algorithm { get; set; } = CryptoAlgorithm.TripleDes;
}

public class BatchKeyCheckResult
{
    public int TotalKeys { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }

    public List<KeyCheckResult> Details { get; set; } = new List<KeyCheckResult>();

    public BatchKeyCheckResult(List<KeyCheckResult> details)
    {
        Details = details;
        TotalKeys = details.Count;
        ValidCount = details.Count(x => x.IsValid);
        InvalidCount = details.Count(x => !x.IsValid);
    }
}
