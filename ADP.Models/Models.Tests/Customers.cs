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
1,66673,Waad Husain,07901565916 / 07702517177,N/A,Al-Qahira,Baghdad,Iraq, ,other,2,M,, 
1,66674,Mohammed Abu Mukhaled,  / N/A / 07732665430,N/A,N/A,Baghdad,Iraq, , ,1, ,, 
1,66675,Khudair Abu Hassan,  / N/A / 07707589982,N/A,N/A,Baghdad,Iraq, , ,1, ,, 
1,66676,Mohammed Ahmed Salim,  / 07504510480,N/A,N/A,Erbil,Iraq, , ,3,M,, 
1,66677,Ahmed Khalil,  / N/A / 07709200464,N/A,N/A,Baghdad,Iraq, , ,1, ,, 
""");

        Assert.Equal(2, parsed.FirstOrDefault()!.Phones.Count);
        Assert.Single(parsed.LastOrDefault()!.Phones);

        engine.Dispose();
    }
}