using System.ComponentModel;

namespace Paymentcardtools.Models.Enum;

public enum CryptoAlgorithm
{
    [Description("3DES - 128/192 bit")]
    TripleDes = 1,

    [Description("DES - 64 bit")]
    Des = 2
}
