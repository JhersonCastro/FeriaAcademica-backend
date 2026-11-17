using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Institutions;

namespace FeriaAcademica.Domain.Tests;

public class InstitutionTests
{
    [Fact]
    public void A_new_institution_is_active()
    {
        Assert.True(Institution.Create("Universidad del Cauca", "Unicauca").IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void The_name_is_required(string name)
    {
        Assert.Throws<DomainException>(() => Institution.Create(name));
    }

    [Fact]
    public void The_name_and_acronym_are_trimmed()
    {
        var institution = Institution.Create("  Universidad del Cauca  ", "  Unicauca ");

        Assert.Equal("Universidad del Cauca", institution.Name);
        Assert.Equal("Unicauca", institution.Acronym);
    }

    [Fact]
    public void The_name_cannot_exceed_200_characters()
    {
        Assert.Equal(200, Institution.Create(new string('a', 200)).Name.Length);
        Assert.Throws<DomainException>(() => Institution.Create(new string('a', 201)));
    }

    [Fact]
    public void An_empty_acronym_is_stored_as_null()
    {
        Assert.Null(Institution.Create("Universidad", "   ").Acronym);
    }

    [Fact]
    public void The_acronym_cannot_exceed_30_characters()
    {
        Assert.Equal(30, Institution.Create("Universidad", new string('a', 30)).Acronym!.Length);
        Assert.Throws<DomainException>(() => Institution.Create("Universidad", new string('a', 31)));
    }

    [Fact]
    public void Updating_changes_the_name_and_acronym_but_not_the_status()
    {
        var institution = Institution.Create("Universidad", "U");
        institution.Deactivate();

        institution.Update("Universidad del Cauca", null);

        Assert.Equal("Universidad del Cauca", institution.Name);
        Assert.Null(institution.Acronym);
        Assert.False(institution.IsActive);
    }

    [Fact]
    public void Updating_applies_the_same_rules_as_creating()
    {
        var institution = Institution.Create("Universidad");

        Assert.Throws<DomainException>(() => institution.Update(" ", null));
        Assert.Equal("Universidad", institution.Name);
    }

    [Fact]
    public void An_institution_can_be_deactivated_and_activated_again()
    {
        var institution = Institution.Create("Universidad");

        institution.Deactivate();
        Assert.False(institution.IsActive);
        institution.Activate();
        Assert.True(institution.IsActive);
    }

    [Fact]
    public void Deactivating_an_inactive_institution_is_rejected()
    {
        var institution = Institution.Create("Universidad");
        institution.Deactivate();

        Assert.Throws<DomainException>(institution.Deactivate);
    }

    [Fact]
    public void Activating_an_active_institution_is_rejected()
    {
        Assert.Throws<DomainException>(Institution.Create("Universidad").Activate);
    }
}
