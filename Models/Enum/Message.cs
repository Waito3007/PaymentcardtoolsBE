using System.ComponentModel;

namespace Paymentcardtools.Models.Enum;

public enum Message
{
    [Description("Key không được để trống")]
    Emptykey = 1,
    [Description("Chứa ký tự không phải Hex")]
    InvalidFormat = 2,
    [Description("Độ dài không đúng chuẩn")]
    InvalidLength = 3,
    [Description("Sai bit chẵn lẻ (Odd Parity)")]
    BadParity = 4,
    [Description("Key hợp lệ")]
    Valid = 5,
    [Description("Key yếu hoặc lỗi thuật toán")]
    WeakKey = 6

}