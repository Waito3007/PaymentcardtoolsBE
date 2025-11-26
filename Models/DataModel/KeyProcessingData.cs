using System;

namespace Paymentcardtools.Models.DataModel;

public class KeyProcessingData
{
    public string KeyHex { get; set; } = string.Empty;
    public string? Cid { get; set; }
    public int RowIndex { get; set; }
}
