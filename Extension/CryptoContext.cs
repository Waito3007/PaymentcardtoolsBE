using Paymentcardtools.Extension.Interface;
using Paymentcardtools.Extension.Strategies;
using Paymentcardtools.Models.Enum;

namespace Paymentcardtools.Extension;

public class CryptoContext
{
    private ICryptoStrategy _strategy;

    public CryptoContext(CryptoAlgorithm algorithm)
    {
        SetStrategy(algorithm);
    }

    public void SetStrategy(CryptoAlgorithm algorithm)
    {
        _strategy = algorithm switch
        {
            CryptoAlgorithm.Des => new DesStrategy(),
            CryptoAlgorithm.TripleDes => new TripleDesStrategy(),
            _ => throw new ArgumentException("Thuật toán mã hóa không được hỗ trợ."),
        };
    }

    public ICryptoStrategy CurrentStrategy => _strategy;
}
