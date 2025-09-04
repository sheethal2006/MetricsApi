using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MetricsApi.Models
{
    public class MetricReading
    {
        public long Id { get; set; }
        public string SensorId { get; set; } = null!;
        public string MetricType { get; set; } = null!; // e.g., "temperature", "humidity"
        public double Value { get; set; }
        public DateTime Timestamp { get; set; } // store in UTC
    }
}
