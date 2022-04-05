using System;

namespace com.zeptile.badminator.Models;

public class BadmintonAvailability
{
    public string Code { get; set; }
    public string Site { get; set; }
    public string Schedule { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public BadmintonAvailabilityStatus Status { get; set; }
}

public enum BadmintonAvailabilityStatus
{
    Full,
    Current,
    Coming,
    Finished,
    MoreInformation,
    Unknown
}