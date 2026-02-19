using System.Runtime.Serialization;

namespace DUS.Contracts
{
    [DataContract]
    public class SensorConfigDto
    {
        [DataMember] public int SensorId { get; set; }
        [DataMember] public string SensorName { get; set; }
        [DataMember] public double MinTemp { get; set; }
        [DataMember] public double MaxTemp { get; set; }
        [DataMember] public DataQuality DefaultQuality { get; set; }
        [DataMember] public double Alarm1 { get; set; }
        [DataMember] public double Alarm2 { get; set; }
        [DataMember] public double Alarm3 { get; set; }

        public SensorConfigDto()
        {
            SensorName = "";
            DefaultQuality = DataQuality.GOOD;
        }
    }

    // Response DTOs
    [DataContract]
    public class RegisterPayload
    {
        [DataMember] public SensorConfigDto Config { get; set; }
        [DataMember] public string ClientPublicKeyXml { get; set; }

        public RegisterPayload()
        {
            Config = new SensorConfigDto();
            ClientPublicKeyXml = "";
        }
    }

    [DataContract]
    public class HeartbeatPayload
    {
        [DataMember] public string Note { get; set; }
        public HeartbeatPayload() { Note = ""; }
    }

    [DataContract]
    public class MeasurementPayload
    {
        //  senzor ID
        [DataMember] public int SensorId { get; set; }

        // vrednost temperature
        [DataMember] public double Value { get; set; }

        //  kvalitet GOOD/BAD/UNCERTAIN
        [DataMember] public DataQuality Quality { get; set; }

        //  alarm prioritet 0/1/2/3
        [DataMember] public AlarmPriority Priority { get; set; }

        // vreme merenja (radi baze/izveštaja)
        [DataMember] public long MeasuredAtUnixMs { get; set; }
    }

    [DataContract]
    public class ReportRequestPayload
    {
        [DataMember] public long FromUnixMs { get; set; }
        [DataMember] public long ToUnixMs { get; set; }
    }

    [DataContract]
    public class AlarmReportItemDto
    {
        // na kom senzoru/klijentu se desilo + kada + prioritet
        [DataMember] public string ClientId { get; set; }
        [DataMember] public int SensorId { get; set; }
        [DataMember] public AlarmPriority Priority { get; set; }
        [DataMember] public double Value { get; set; }
        [DataMember] public long TimeUnixMs { get; set; }

        public AlarmReportItemDto()
        {
            ClientId = "";
        }
    }

}
