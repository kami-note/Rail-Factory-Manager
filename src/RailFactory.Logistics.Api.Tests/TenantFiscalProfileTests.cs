using FluentAssertions;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="TenantFiscalProfile"/> entity.
/// </summary>
public class TenantFiscalProfileTests
{
    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        // Act
        var profile = TenantFiscalProfile.Create(
            "5101", "6101", "SP", 18.0m, "00", "01", "01", 5.0m, 0,
            "ACME Corp", "12.345.678/0001-90", "111.222.333.444", "São Paulo", "SP"
        );

        // Assert
        profile.Id.Should().Be("default");
        profile.CfopPadraoIntraestadual.Should().Be("5101");
        profile.CfopPadraoInterestadual.Should().Be("6101");
        profile.UfOrigem.Should().Be("SP");
        profile.IcmsRate.Should().Be(18.0m);
        profile.IcmsCst.Should().Be("00");
        profile.PisCst.Should().Be("01");
        profile.CofinsCst.Should().Be("01");
        profile.IpiRate.Should().Be(5.0m);
        profile.IcmsOrigin.Should().Be(0);
        profile.EmitterName.Should().Be("ACME Corp");
        profile.EmitterCnpj.Should().Be("12.345.678/0001-90");
        profile.EmitterIe.Should().Be("111.222.333.444");
        profile.EmitterCity.Should().Be("São Paulo");
        profile.EmitterState.Should().Be("SP");
    }

    [Fact]
    public void Update_ShouldModifyPropertiesCorrectly()
    {
        // Arrange
        var profile = TenantFiscalProfile.Create(
            "5101", "6101", "SP", 18.0m, "00", "01", "01", 5.0m, 0,
            "ACME Corp", "12.345.678/0001-90", "111.222.333.444", "São Paulo", "SP"
        );

        // Act
        profile.Update(
            "5102", "6102", "RJ", 12.0m, "40", "07", "07", 0m, 1,
            "ACME Rio", "98.765.432/0001-10", "999.888.777.666", "Rio de Janeiro", "RJ"
        );

        // Assert
        profile.CfopPadraoIntraestadual.Should().Be("5102");
        profile.CfopPadraoInterestadual.Should().Be("6102");
        profile.UfOrigem.Should().Be("RJ");
        profile.IcmsRate.Should().Be(12.0m);
        profile.IcmsCst.Should().Be("40");
        profile.PisCst.Should().Be("07");
        profile.CofinsCst.Should().Be("07");
        profile.IpiRate.Should().Be(0m);
        profile.IcmsOrigin.Should().Be(1);
        profile.EmitterName.Should().Be("ACME Rio");
        profile.EmitterCnpj.Should().Be("98.765.432/0001-10");
        profile.EmitterIe.Should().Be("999.888.777.666");
        profile.EmitterCity.Should().Be("Rio de Janeiro");
        profile.EmitterState.Should().Be("RJ");
    }
}
