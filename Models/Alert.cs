using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MetricsApi.Models
{
    public class Alert
    {
        public long Id { get; set; }
        public string SensorId { get; set; } = null!;
        public string MetricType { get; set; } = null!;
        public string AlertType { get; set; } = null!; // e.g., "High Temperature"
        public double Value { get; set; }
        public double Threshold { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
