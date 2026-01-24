using System;

namespace DUS.Contracts
{
    public enum DataQuality { GOOD = 0, BAD = 1, UNCERTAIN = 2 }
    public enum AlarmPriority { P0_None = 0, P1 = 1, P2 = 2, P3 = 3 }
    public enum ClientRole { Standby = 0, Active = 1 }
    public enum MessageType { Register = 0, Heartbeat = 1, Measurement = 2, ReportRequest = 3 }
}
