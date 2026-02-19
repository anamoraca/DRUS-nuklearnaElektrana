using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DUS.Contracts
{
    [DataContract]
    public class RegisterResponse
    {
        [DataMember] public bool Ok { get; set; }
        [DataMember] public string Error { get; set; }
        [DataMember] public ClientRole Role { get; set; }
        [DataMember] public string AssignedConsoleColor { get; set; }

        public RegisterResponse()
        {
            Error = "";
            AssignedConsoleColor = "Gray";
        }
    }

    [DataContract]
    public class HeartbeatResponse
    {
        //  server vraća rolu (Active/Standby) + boju active senzoru
        [DataMember] public bool Ok { get; set; }
        [DataMember] public string Error { get; set; }
        [DataMember] public ClientRole Role { get; set; }
        [DataMember] public string AssignedConsoleColor { get; set; }

        public HeartbeatResponse()
        {
            Error = "";
            AssignedConsoleColor = "Gray";
        }
    }

    [DataContract]
    public class AckResponse
    {
        [DataMember] public bool Ok { get; set; }
        [DataMember] public string Error { get; set; }
        public AckResponse() { Error = ""; }
    }

    [DataContract]
    public class AlarmReportResponse
    {
        [DataMember] public bool Ok { get; set; }
        [DataMember] public string Error { get; set; }
        [DataMember] public List<AlarmReportItemDto> Items { get; set; }

        public AlarmReportResponse()
        {
            Error = "";
            Items = new List<AlarmReportItemDto>();
        }
    }
}
