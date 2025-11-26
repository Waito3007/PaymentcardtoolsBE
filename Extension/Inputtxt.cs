using System;
using Paymentcardtools.Extension.Interface;
using Paymentcardtools.Models.DataModel;

namespace Paymentcardtools.Extension;

public class Inputtxt : IInputSource
{
    public async Task<List<KeyProcessingData>> ExtractKeyAsync(object input)
    {
        var keys = new List<KeyProcessingData>();
        var stream = input as System.IO.Stream;
        using (var reader = new StreamReader(stream))
        {
            string line;
            int rowIndex = 1;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var cleanedline = line.Trim();
                if (!string.IsNullOrEmpty(cleanedline))
                {
                    keys.Add(new KeyProcessingData
                    {
                        KeyHex = cleanedline,
                        Cid = null,
                        RowIndex = rowIndex
                    });
                    rowIndex++;
                }
            }
        }
        return keys;
    }
}