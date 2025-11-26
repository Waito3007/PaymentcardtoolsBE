using System;
using Paymentcardtools.Models.DataModel;

namespace Paymentcardtools.Extension.Interface;

public interface IInputSource
{
    Task<List<KeyProcessingData>> ExtractKeyAsync(object inputSource);
}
