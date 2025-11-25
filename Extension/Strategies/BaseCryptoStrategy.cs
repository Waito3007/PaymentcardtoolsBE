using System.Text.RegularExpressions;
using Paymentcardtools.Extension.Interface;
using Paymentcardtools.Models.DataModel;
using Paymentcardtools.Models.Enum;

namespace Paymentcardtools.Extension.Strategies
{
    public abstract class BaseCryptoStrategy : ICryptoStrategy
    {
        public KeyCheckResult ValidateKeyStructure(string rawKey)
        {
            var result = new KeyCheckResult { InputKey = rawKey, Status = KeyValidationStatus.Valid };

            if (string.IsNullOrWhiteSpace(rawKey))
            {
                result.Status = KeyValidationStatus.InvalidFormat;
                result.Message = Message.Emptykey.GetDescriptionOfEnum();
                return result;
            }

            if (!Regex.IsMatch(rawKey, "^[0-9a-fA-F]+$"))
            {
                result.Status = KeyValidationStatus.InvalidFormat;
                result.Message = Message.InvalidFormat.GetDescriptionOfEnum();
                return result;
            }

            return ValidateSpecificRules(rawKey, result);
        }

        protected abstract KeyCheckResult ValidateSpecificRules(string key, KeyCheckResult result);

        public abstract string CalculateKcv(string hexKey);

        protected bool IsOddParity(string hexKey)
        {
            byte[] bytes = Convert.FromHexString(hexKey);
            foreach (var b in bytes)
            {
                int setBits = 0;
                int n = b;
                while (n > 0)
                {
                    n &= (n - 1); setBits++;
                }
                if (setBits % 2 == 0) return false;
            }
            return true;
        }
    }
}