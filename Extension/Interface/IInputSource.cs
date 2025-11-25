using System;

namespace Paymentcardtools.Extension.Interface;

public interface IInputSource
{
    Task<List<string>> ExtractKeyAsync(object inputSource);
}
