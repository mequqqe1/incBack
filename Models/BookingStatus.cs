namespace SharpAuthDemo.Models;

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    Declined = 2,
    CancelledByParent = 3,
    CancelledBySpecialist = 4
}