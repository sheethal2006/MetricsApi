namespace MetricsApi.DTOs
{      //dto - do not want to expose all properties of our entity to the API
       public record MetricReadingDto
       (
       string SensorId,
       string MetricType,
       double Value
       //DateTime? Timestamp // if null server will set DateTime.UtcNow
       );
}
