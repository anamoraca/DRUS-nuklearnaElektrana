using System.ServiceModel;

namespace DUS.Contracts
{
    [ServiceContract]
    public interface ITemperatureService
    {
        [OperationContract] RegisterResponse Register(SecureEnvelope request);
        [OperationContract] HeartbeatResponse Heartbeat(SecureEnvelope request);
        [OperationContract] AckResponse SendMeasurement(SecureEnvelope request);
        [OperationContract] AlarmReportResponse GetAlarmReport(SecureEnvelope request);
    }
}
