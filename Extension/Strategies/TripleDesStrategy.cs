using System.Security.Cryptography;
using Paymentcardtools.Extension.Interface;
using Paymentcardtools.Models.DataModel;
using Paymentcardtools.Models.Enum;

namespace Paymentcardtools.Extension.Strategies;

public class TripleDesStrategy : BaseCryptoStrategy
{
    public override string CalculateKcv(string hexKey)
    {
        try
        {
            byte[] keyBytes = Convert.FromHexString(hexKey);
            using var tdes = TripleDES.Create();
            tdes.Key = keyBytes;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.None;

            using var encryptor = tdes.CreateEncryptor();
            byte[] zeroBlock = new byte[8];
            byte[] result = encryptor.TransformFinalBlock(zeroBlock, 0, 8);

            return Convert.ToHexString(result)[..6];
        }
        catch
        {
            throw new ArgumentException("Key cho 3DES không hợp lệ");
        }
    }

    protected override KeyCheckResult ValidateSpecificRules(string key, KeyCheckResult result)
    {
        // 3DES Length 32 hoặc 48
        if (key.Length != 32 && key.Length != 48)
        {
            result.Status = KeyValidationStatus.InvalidLength;
            result.Message = Message.InvalidLength.GetDescriptionOfEnum();
            return result;
        }

        if (!IsOddParity(key))
        {
            result.Status = KeyValidationStatus.BadParity;
            result.Message = Message.BadParity.GetDescriptionOfEnum();
            return result;
        }

        return result;
    }
}