using System;

namespace DUS.Contracts
{
   
    //  kvalitet podataka GOOD/BAD/UNCERTAIN
    public enum DataQuality { GOOD = 0, BAD = 1, UNCERTAIN = 2 }

    //  prioriteti alarma 1,2,3 + 0 za "nema alarma"
    public enum AlarmPriority { P0_None = 0, P1 = 1, P2 = 2, P3 = 3 }

    //  10 klijenata => 5 Active + 5 Standby
    public enum ClientRole { Standby = 0, Active = 1 }

    // Tip poruke 
    public enum MessageType { Register = 0, Heartbeat = 1, Measurement = 2, ReportRequest = 3 }
}
