using System.Reflection;
using ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Shared.Tests;

/// <summary>
/// The golden-customer DTOs are the PII contract the API protects and the reveal route allows.
/// A member turned back into a string would ship raw values in every response, so these
/// declarations are pinned here.
/// </summary>
public class GoldenCustomerPiiContractTests
{
    public static TheoryData<Type, string, PiiKind> ProtectedMembers => new()
    {
        { typeof(GoldenCustomerListDTO), nameof(GoldenCustomerListDTO.FullName), PiiKind.Name },
        { typeof(GoldenCustomerListDTO), nameof(GoldenCustomerListDTO.Phone), PiiKind.Phone },
        { typeof(GoldenCustomerListDTO), nameof(GoldenCustomerListDTO.Email), PiiKind.Email },
        { typeof(GoldenCustomerListDTO), nameof(GoldenCustomerListDTO.IDNumber), PiiKind.Identifier },
        { typeof(GoldenCustomerDetailDTO), nameof(GoldenCustomerDetailDTO.FullName), PiiKind.Name },
        { typeof(GoldenCustomerDetailDTO), nameof(GoldenCustomerDetailDTO.Phone), PiiKind.Phone },
        { typeof(GoldenCustomerDetailDTO), nameof(GoldenCustomerDetailDTO.Email), PiiKind.Email },
        { typeof(GoldenCustomerDetailDTO), nameof(GoldenCustomerDetailDTO.IDNumber), PiiKind.Identifier },
    };

    [Theory]
    [MemberData(nameof(ProtectedMembers))]
    public void Member_IsARevealableProtectedField(Type dto, string member, PiiKind kind)
    {
        var property = dto.GetProperty(member)!;
        var declaration = property.GetCustomAttribute<PiiAttribute>();

        Assert.Equal(typeof(PiiFieldDTO), property.PropertyType);
        Assert.True(property.CanWrite);
        Assert.NotNull(declaration);
        Assert.Equal(kind, declaration.Kind);
        Assert.True(declaration.Revealable);
    }

    [Theory]
    [InlineData(typeof(GoldenCustomerListDTO))]
    [InlineData(typeof(GoldenCustomerDetailDTO))]
    public void OnlyNamePhoneEmailAndNationalId_AreProtected(Type dto)
    {
        var protectedMembers = dto.GetProperties()
            .Where(x => x.PropertyType == typeof(PiiFieldDTO))
            .Select(x => x.Name)
            .OrderBy(x => x, StringComparer.Ordinal);

        Assert.Equal(["Email", "FullName", "IDNumber", "Phone"], protectedMembers);
        Assert.Equal(typeof(string), dto.GetProperty("City")!.PropertyType);
    }
}
