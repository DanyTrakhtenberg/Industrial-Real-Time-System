Create tests for SensorTelemetryGrpcService.

Requirements:

* Use xUnit
* Use EF Core InMemory database OR SQLite in-memory
* Create a dedicated test project:
  tests/SqlDataService.Tests

Add tests for:

1. GetSensors returns seeded sensors ordered by Id
2. GetSensorById returns Found=true for existing sensor
3. GetSensorById returns Found=false for missing sensor
4. SaveTelemetry saves a telemetry row successfully
5. SaveTelemetry throws RpcException with StatusCode.NotFound when sensor does not exist
6. GetTelemetryHistory returns telemetry ordered by newest first
7. GetTelemetryHistory supports SensorId filtering
8. GetTelemetryHistory supports pagination/PageToken

Requirements:

* Create reusable test helpers for:

  * ApplicationDbContext creation
  * test data seeding
  * fake ServerCallContext
* Keep tests deterministic
* Do not use a real SQL server yet
* Do not use Docker yet
* Tests must compile and run with dotnet test

After creating tests:

1. Run all tests
2. Fix compile/runtime issues
3. Ensure all tests pass

Do not refactor production code unless necessary to make tests testable.
If production changes are required, explain why before changing them.
