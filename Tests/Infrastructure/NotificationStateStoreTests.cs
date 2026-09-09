using Infrastructure.State;
using Shouldly;
using Xunit;

namespace Tests.Infrastructure;

public class NotificationStateStoreTests
{
    private readonly NotificationStateStore _stateStore = new();

    [Fact]
    public void InitialState_ShouldHaveAllFlagsFalse()
    {
        // Assert
        _stateStore.HasPendingNotifications.ShouldBeFalse();
        _stateStore.HasNotifiedAdmin.ShouldBeFalse();
    }

    [Fact]
    public void SetPendingStatus_True_ShouldSetHasPendingNotificationsToTrue()
    {
        // Act
        _stateStore.SetPendingStatus(true);

        // Assert
        _stateStore.HasPendingNotifications.ShouldBeTrue();
        _stateStore.HasNotifiedAdmin.ShouldBeFalse();
    }

    [Fact]
    public void SetAdminNotified_True_ShouldSetHasNotifiedAdminToTrue()
    {
        // Act
        _stateStore.SetAdminNotified(true);

        // Assert
        _stateStore.HasNotifiedAdmin.ShouldBeTrue();
    }

    [Fact]
    public void SetPendingStatus_False_ShouldResetHasNotifiedAdminToFalse()
    {
        // Arrange
        _stateStore.SetPendingStatus(true);
        _stateStore.SetAdminNotified(true);

        // Act
        _stateStore.SetPendingStatus(false);

        // Assert
        _stateStore.HasPendingNotifications.ShouldBeFalse();
        _stateStore.HasNotifiedAdmin.ShouldBeFalse();
    }

    [Fact]
    public void Reset_ShouldResetAllFlagsToFalse()
    {
        // Arrange
        _stateStore.SetPendingStatus(true);
        _stateStore.SetAdminNotified(true);

        // Act
        _stateStore.Reset();

        // Assert
        _stateStore.HasPendingNotifications.ShouldBeFalse();
        _stateStore.HasNotifiedAdmin.ShouldBeFalse();
    }
}