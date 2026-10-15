using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Notifications;

namespace FeriaAcademica.Domain.Tests;

public class NotificationTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Recipient = Guid.NewGuid();

    private static Notification NewNotification() => Notification.Create(Recipient, null, "WorkReviewed", "Your work was approved.", T0);

    [Fact]
    public void A_notification_starts_unread()
    {
        Assert.False(NewNotification().IsRead);
    }

    [Fact]
    public void The_recipient_can_mark_it_as_read()
    {
        var notification = NewNotification();

        notification.MarkAsRead(Recipient);

        Assert.True(notification.IsRead);
    }

    [Fact]
    public void Only_the_recipient_can_mark_it_as_read()
    {
        var notification = NewNotification();

        Assert.Throws<DomainException>(() => notification.MarkAsRead(Guid.NewGuid()));
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void A_notification_requires_type_and_message()
    {
        Assert.Throws<DomainException>(() => Notification.Create(Recipient, null, " ", "Message.", T0));
        Assert.Throws<DomainException>(() => Notification.Create(Recipient, null, "WorkReviewed", "", T0));
    }
}
