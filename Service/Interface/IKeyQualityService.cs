using System;
using Paymentcardtools.Models.DataModel;
using Paymentcardtools.Models.Enum;

namespace Paymentcardtools.Service.Interface;

public interface IKeyQualityService
{
    void SetAlgorithm(CryptoAlgorithm algorithm);
    KeyCheckResult ValidateKey(string rawKey);
    BatchKeyCheckResult ValidateBatch(List<string> rawKeys);
}
