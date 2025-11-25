using System.Security.Cryptography;
using Paymentcardtools.Extension.Interface;
using Paymentcardtools.Models.DataModel;
using Paymentcardtools.Models.Enum;

namespace Paymentcardtools.Extension.Strategies;

public class DesStrategy : BaseCryptoStrategy
{
    public override string CalculateKcv(string hexKey)
    {
        try
        {
            byte[] keyBytes = Convert.FromHexString(hexKey);

            if (keyBytes.Length != 8)
            {
                throw new ArgumentException("Key cho DES phải có 16 ký tự hex");
            }

            using var des = DES.Create();
            des.Key = keyBytes;
            des.Mode = CipherMode.ECB;
            des.Padding = PaddingMode.None;

            using var encryptor = des.CreateEncryptor();
            byte[] zeroBlock = new byte[8];
            byte[] result = encryptor.TransformFinalBlock(zeroBlock, 0, 8);

            return Convert.ToHexString(result)[..6];
        }
        catch
        {
            throw new ArgumentException("Key cho DES không hợp lệ");
        }
    }

    protected override KeyCheckResult ValidateSpecificRules(string key, KeyCheckResult result)
    {
        if (key.Length != 16)
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
