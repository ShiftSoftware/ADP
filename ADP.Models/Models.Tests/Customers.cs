using ShiftSoftware.ADP.Models.DealerData;
using ShiftSoftware.ADP.Models;

namespace Models.Tests;

public class Customers
{
    [Fact]
    public void ParseCustomers()
    {
        var engine = new CacheableCSVEngine<CustomerDataCSV>();

        var parsed = engine.ReadStringAsList(
"""
DealerId,MagicNumber,Customer_Name,Phone_No,Address001,Address002,Address003,Address004,Address005,JobTitle,LanguageCodeC,Gender,DateOfBirth,WorkName
1,66673,Customer One,07900000001 / 07900000002,N/A,District 1,City A,Country A, ,other,2,M,,
1,66674,Customer Two,  / N/A / 07900000003,N/A,N/A,City A,Country A, , ,1, ,,
1,66675,Customer Three,  / N/A / 07900000004,N/A,N/A,City A,Country A, , ,1, ,,
1,66676,Customer Four,  / 07900000005,N/A,N/A,City B,Country A, , ,3,M,,
1,66677,Customer Five,  / N/A / 07900000006,N/A,N/A,City A,Country A, , ,1, ,,
""");

        Assert.Equal(2, parsed.FirstOrDefault()!.Phones.Count);
        Assert.Single(parsed.LastOrDefault()!.Phones);

        engine.Dispose();
    }
}