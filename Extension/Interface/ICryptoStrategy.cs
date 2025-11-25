using Paymentcardtools.Models.DataModel;

namespace Paymentcardtools.Extension.Interface;

public interface ICryptoStrategy
{
    string CalculateKcv(string hexKey);
    KeyCheckResult ValidateKeyStructure(string rawKey);
}