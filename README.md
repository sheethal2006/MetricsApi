# MetricsApi - Take-Home Exercise

#### Prerequisites
* .NET SDK 6.0 or higher
* PostgreSQL database instance (local or containerized)

######Implementation Steps######

1.Created an ASP.NET Core Minimal API project and set up PostgreSQL.

2.Implemented POST endpoint to ingest and store metric readings.

3.Created GET endpoint to:
Retrieve aggregated metric summaries.
Retrieve triggered alerts.

4.Used clean separation of layers (Minimal API + Services + DTOs).

5.Implemented basic validation and graceful handling for “no data” scenarios.

6.Leveraged dependency injection for logging.

7.Added Swagger/OpenAPI for interactive API testing.

8.Included a Docker Compose file for easy local setup.

##########Steps to run the code from gitHub##########
1. git clone https://github.com/sheethal2006/MetricsApi
2. dotnet build
3. dotnet run --project MetricsApi
4. The API will be available at:
http://localhost:5000
   
#############End Points ##########
#### 1. Ingest a new metric reading (POST)
```bash
curl -X POST http://localhost:5000/api/metrics \
-H "Content-Type: application/json" \
-d '{
  "SensorId": "temp-sensor-1",
  "MetricType": "temperature",
  "Value": 45.0
}'

Get a summary of metrics (GET)
curl "http://localhost:5000/api/metrics/summary?metricType=humidity&interval=1h"

Retrieve recent alerts (GET)
curl "http://localhost:5000/api/alerts"

###########Containerization########
docker build -t metricsApi .
docker run -d -p 8080:80 metricsApi

The API will then be available at:
http://localhost:8080

#####Future Improvements#########

Add unit tests using xUnit & Moq

Add logging & monitoring (Serilog / Application Insights)

Add authentication & authorization for secure access

CI/CD pipeline integration for automated builds and deployment

Try to integrate this solution with event hub and AZ service bus.

Connectionstring should be in AZ KeyVault for safty.

##########Notes#########
The project focuses on functionality and code clarity rather than production-hardening.
Thresholds and business rules are configured in appsettings.json.
No authentication was added, per the exercise requirements.

