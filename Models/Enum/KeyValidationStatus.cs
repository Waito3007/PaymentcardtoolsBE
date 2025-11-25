using System.ComponentModel;

namespace Paymentcardtools.Models.Enum;

public enum KeyValidationStatus
{
    [Description("Key hợp lệ")]
    Valid = 1,
    [Description("Lỗi format chứa ký tự ko phải hex")]
    InvalidFormat = 2,
    [Description("Lỗi độ dài")]
    InvalidLength = 3,
    [Description("Sai bit chẵn lẻ ")]
    BadParity = 4,
    [Description("Key yếu, dễ đoán")]
    WeakKey = 5
}
