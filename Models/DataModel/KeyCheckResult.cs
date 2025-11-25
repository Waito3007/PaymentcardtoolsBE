using System;
using Paymentcardtools.Models.Enum;

namespace Paymentcardtools.Models.DataModel;

public class KeyRequest
{
    public string Key { get; set; } = string.Empty;
    public CryptoAlgorithm Algorithm { get; set; } = CryptoAlgorithm.TripleDes;
}

public class KeyCheckResult
{
    public string InputKey { get; set; } = string.Empty;
    public bool IsValid => Status == KeyValidationStatus.Valid;
    public KeyValidationStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Kcv { get; set; }
}