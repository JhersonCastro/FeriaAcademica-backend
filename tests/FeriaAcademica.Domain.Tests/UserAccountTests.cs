using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Users;

namespace FeriaAcademica.Domain.Tests;

public class UserAccountTests
{
    private static readonly Guid User = Guid.NewGuid();
    private static readonly Guid Admin = Guid.NewGuid();

    // ---- Sign in ----

    [Fact]
    public void An_active_account_can_sign_in() => UserAccount.EnsureCanSignIn(UserStatus.Active);

    [Fact]
    public void An_inactive_account_cannot_sign_in()
    {
        var ex = Assert.Throws<DomainException>(() => UserAccount.EnsureCanSignIn(UserStatus.Inactive));
        Assert.Contains("deactivated", ex.Message);
    }

    [Fact]
    public void A_pending_account_cannot_sign_in_until_it_sets_its_password()
    {
        var ex = Assert.Throws<DomainException>(() => UserAccount.EnsureCanSignIn(UserStatus.Pending));
        Assert.Contains("password", ex.Message);
    }

    // ---- Deactivate ----

    [Theory]
    [InlineData(UserStatus.Active)]
    [InlineData(UserStatus.Pending)]
    public void Deactivate_makes_the_account_inactive(UserStatus current) =>
        Assert.Equal(UserStatus.Inactive, UserAccount.Deactivate(current, User, Admin));

    [Fact]
    public void Nobody_can_deactivate_their_own_account() =>
        Assert.Throws<DomainException>(() => UserAccount.Deactivate(UserStatus.Active, Admin, Admin));

    [Fact]
    public void An_inactive_account_cannot_be_deactivated_again() =>
        Assert.Throws<DomainException>(() => UserAccount.Deactivate(UserStatus.Inactive, User, Admin));

    // ---- Activate ----

    [Fact]
    public void Activate_returns_an_account_with_password_to_Active() =>
        Assert.Equal(UserStatus.Active, UserAccount.Activate(UserStatus.Inactive, hasPassword: true));

    [Fact]
    public void Activate_returns_an_account_without_password_to_Pending() =>
        Assert.Equal(UserStatus.Pending, UserAccount.Activate(UserStatus.Inactive, hasPassword: false));

    [Theory]
    [InlineData(UserStatus.Active)]
    [InlineData(UserStatus.Pending)]
    public void Only_an_inactive_account_can_be_activated(UserStatus current) =>
        Assert.Throws<DomainException>(() => UserAccount.Activate(current, hasPassword: true));

    // ---- Password ----

    [Theory]
    [InlineData(UserStatus.Pending)]
    [InlineData(UserStatus.Active)]
    public void Setting_a_password_leaves_the_account_Active(UserStatus current) =>
        Assert.Equal(UserStatus.Active, UserAccount.AfterPasswordSet(current));

    [Fact]
    public void An_inactive_account_cannot_reactivate_itself_by_setting_a_password() =>
        Assert.Throws<DomainException>(() => UserAccount.AfterPasswordSet(UserStatus.Inactive));

    [Fact]
    public void An_inactive_account_cannot_recover_its_password()
    {
        Assert.False(UserAccount.CanRecoverPassword(UserStatus.Inactive));
        Assert.True(UserAccount.CanRecoverPassword(UserStatus.Active));
        Assert.True(UserAccount.CanRecoverPassword(UserStatus.Pending));
    }
}
