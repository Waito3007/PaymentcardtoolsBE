using System;
using Paymentcardtools.Extension.Interface;

namespace Paymentcardtools.Extension;

public class Inputtxt : IInputSource
{
    public async Task<List<string>> ExtractKeyAsync(object input)
    {
        var keys = new List<string>();
        var stream = input as System.IO.Stream;
        using (var reader = new StreamReader(stream))
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var cleanedline = line.Trim();
                if (!string.IsNullOrEmpty(cleanedline))
                {
                    keys.Add(cleanedline);
                }
            }
        }
        return keys;
    }
}