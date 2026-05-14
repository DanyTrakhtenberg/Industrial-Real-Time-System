Create docker-compose.yml.

Services:
- ui
- rest-api
- sql-data-service
- telemetry-service
- redis
- rabbitmq
- postgres or sqlserver

Each custom service must have its own Dockerfile.
Make sure service names match connection strings.