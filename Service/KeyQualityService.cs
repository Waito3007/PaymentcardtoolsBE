using System.Text.RegularExpressions;
using Paymentcardtools.Extension;
using Paymentcardtools.Extension.Interface;
using Paymentcardtools.Models.DataModel;
using Paymentcardtools.Models.Enum;
using Paymentcardtools.Service.Interface;

namespace Paymentcardtools.Service
{
    public class KeyQualityService : IKeyQualityService
    {
        private readonly CryptoContext _cryptoContext;

        public KeyQualityService()
        {
            _cryptoContext = new CryptoContext(CryptoAlgorithm.TripleDes);
        }

        public void SetAlgorithm(CryptoAlgorithm algorithm)
        {
            _cryptoContext.SetStrategy(algorithm);
        }

        #region Validate Key

        public KeyCheckResult ValidateKey(string rawKey)
        {
            var result = _cryptoContext.CurrentStrategy.ValidateKeyStructure(rawKey);

            if (result.Status != KeyValidationStatus.Valid)
            {
                return result;
            }
            try
            {
                result.Kcv = _cryptoContext.CurrentStrategy.CalculateKcv(rawKey);

                result.Status = KeyValidationStatus.Valid;
                result.Message = Message.Valid.GetDescriptionOfEnum();
            }
            catch (Exception)
            {
                result.Status = KeyValidationStatus.WeakKey;
                result.Message = Message.WeakKey.GetDescriptionOfEnum();
            }

            return result;
        }

        #endregion

        #region Validate Batch

        public BatchKeyCheckResult ValidateBatch(List<string> rawKeys)
        {
            var details = new List<KeyCheckResult>();
            foreach (var key in rawKeys)
            {
                details.Add(ValidateKey(key));
            }

            return new BatchKeyCheckResult(details);
        }

        #endregion
    }
}