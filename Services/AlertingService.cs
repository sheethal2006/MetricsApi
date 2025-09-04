using MetricsApi.Data;
using MetricsApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


namespace MetricsApi.Services
{
    public class AlertingService
    {
        private readonly MetricsDbContext _db;
        private readonly IConfiguration _config;
        private readonly int _consecutiveCount;
        private readonly ILogger<AlertingService> _logger; // Added logger

        public AlertingService(MetricsDbContext db, IConfiguration config, ILogger<AlertingService> logger)
        {
            _db = db;
            _config = config;
            _consecutiveCount = _config.GetValue<int>("Alerting:ConsecutiveBreachCount", 3);
            _logger = logger;
            
        }

        private double GetThresholdFor(string metricType, out bool isRange, out double min, out double max)
        {
            isRange = false; min = double.NaN; max = double.NaN;
            if (metricType.Equals("temperature", StringComparison.OrdinalIgnoreCase))
            {
                max = _config.GetValue<double>("Thresholds:temperature", 30.0);
                isRange = false;
                return max;
            }
            if (metricType.Equals("humidity", StringComparison.OrdinalIgnoreCase))
            {
                isRange = true;  //isRange → true if metric has both a min & max (like humidity)
                min = _config.GetValue<double>("Thresholds:humidity_min", 20.0);
                max = _config.GetValue<double>("Thresholds:humidity_max", 70.0);
                return max;
            }
            // default: treat as upper-threshold metric
            max = _config.GetValue<double>($"Thresholds:{metricType}", double.NaN);
            return max;

        }

        // Called after saving a reading: checks consecutive-breach rule.
        public async Task CheckAndCreateAlertAsync(MetricReading reading)
        {

            var metricType = reading.MetricType.ToLowerInvariant();
            bool isRange;
            double min, max;
            GetThresholdFor(metricType, out isRange, out min, out max);

            // If no relevant thresholds configured -> skip
            if (double.IsNaN(max) && !isRange) return;

            // Get previous _consecutiveCount -1 readings for same sensor & metric type (most recent)
            var recent = await _db.MetricReadings
                .Where(r => r.SensorId == reading.SensorId && r.MetricType.ToLower() == metricType)
                .OrderByDescending(r => r.Timestamp)
                .Take(_consecutiveCount - 1)
                .ToListAsync();

            // Compose last N readings including the new one (most recent first)
            var values = new List<double> { reading.Value };
            values.AddRange(recent.Select(r => r.Value));

            if (values.Count < _consecutiveCount)
            {
                // not enough readings to evaluate consecutive rule
                return;
            }

            bool breach;
            if (isRange)
            {
                // for humidity: out-of-range is either <min or >max; require consecutive breaches  (any side)
                breach = values.All(v => v < min || v > max);
            }
            else
            {
                // upper threshold only
                breach = values.All(v => v > max);
            }

            if (breach)
            {
                // create alert
                var alert = new Alert
                {
                    SensorId = reading.SensorId,
                    MetricType = metricType,
                    AlertType = $"Consecutive {_consecutiveCount} breach",
                    Value = reading.Value,
                    Threshold = isRange ? max : max, // store max for simplicity; for ranges you may store both
                    Timestamp = DateTime.UtcNow
                };

                _db.Alerts.Add(alert);
                await _db.SaveChangesAsync();

                _logger.LogWarning("Alert created for SensorId: {SensorId}, MetricType: {MetricType}, Value: {Value}",
                 reading.SensorId, metricType, reading.Value);
            }
        }


    }
}
