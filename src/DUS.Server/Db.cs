using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace DUS.Server
{
    public class MeasurementEntity
    {
        [Key] public int Id { get; set; }
        public string ClientId { get; set; }
        public int SensorId { get; set; }
        public DateTime TimestampUtc { get; set; }
        public double Value { get; set; }
        public int Quality { get; set; }
        public int Priority { get; set; }
        public bool IsConsensus { get; set; }

        public MeasurementEntity()
        {
            ClientId = "";
        }
    }

    public class DusDbContext : DbContext
    {
        public DusDbContext() : base("name=DusDb") { }
        public DbSet<MeasurementEntity> Measurements { get; set; }
    }
}
