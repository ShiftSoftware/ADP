namespace ShiftSoftware.ADP.Menus.Shared;

public class Utility
{
    public static string GetAllowedTimeText(decimal allowedTime)
    {
        if (allowedTime < 0)
            throw new ArgumentException("Allowed time cannot be negative");

        var result = allowedTime.ToString().Trim('0');
        result = result.Replace(".", string.Empty);

        if (allowedTime < 1)
            return "0" + result;

        if (result.Length > 1)
            return result;

        return result + "0";
    }

    public static string GetBrandAbbreviation(long? brandID)
    {
        return brandID switch
        {
            1 => "T", // Toyota
            2 => "L", // Lexus
            3 => "H", // Hino
            _ => "Z" // Other/Unknown
        };
    }

    public static string ConvertBrandToMenuCompanyCode(long brandId)
    {
        return brandId switch
        {
            1 => "00", // Toyota
            2 => "11", // Lexus
            _ => throw new NotImplementedException()
        };
    }

    //public static long? ConvertBrandEnumToIdentityId(Franchises? brand)
    //{
    //    return brand switch
    //    {
    //        Franchises.Toyota => 1,
    //        Franchises.Lexus => 2,
    //        Franchises.Hino => 3,
    //        _ => null
    //    };

    //}

    //public static Franchises ConvertBrandIdentityIdToBrandEnum(long? brandId)
    //{
    //    return brandId switch
    //    {
    //        1 => Franchises.Toyota,
    //        2 => Franchises.Lexus,
    //        3 => Franchises.Hino,
    //        _ => throw new NotImplementedException()
    //    };
    //}
}
