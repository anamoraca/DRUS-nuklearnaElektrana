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
        [DataMember] public int SensorId { get; set; }
        [DataMember] public double Value { get; set; }
        [DataMember] public DataQuality Quality { get; set; }
        [DataMember] public AlarmPriority Priority { get; set; }
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
        [DataMember] public int SensorId { get; set; }
        [DataMember] public AlarmPriority Priority { get; set; }
        [DataMember] public double Value { get; set; }
        [DataMember] public long TimeUnixMs { get; set; }
    }
}
