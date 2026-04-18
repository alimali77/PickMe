namespace PickMe.Domain;

public enum UserRole
{
    Customer = 1,
    Driver = 2,
    Admin = 3,
}

public enum ReservationStatus
{
    Pending = 1,
    Assigned = 2,
    OnTheWay = 3,
    Completed = 4,
    Cancelled = 5,
}

public enum ServiceType
{
    Driver = 1,
    Valet = 2,
}

public enum DriverStatus
{
    Active = 1,
    Inactive = 2,
}

public enum CancelledBy
{
    Customer = 1,
    Admin = 2,
    System = 3,
}

public enum EmailLogStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    Retrying = 4,
}
