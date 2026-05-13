Read Industrial_Real_Time_Home_Assignment.pdf carefully.

Do NOT implement the entire system yet.

First:

1. Design the high-level architecture
2. Create the monorepo folder structure
3. Define service responsibilities and boundaries
4. Define communication flows
5. Define shared DTO/contracts

Requirements:

* React + TypeScript UI with exactly 3 pages
* REST API service in C#
* SQL Data Service in C# + EF Core + gRPC
* IoT Telemetry Service in C#
* Redis, RabbitMQ, SQL database
* docker-compose
* tests
* GitHub Actions

Important constraints:

* Backend services communicate ONLY through gRPC and RabbitMQ
* SignalR ONLY between REST API and UI
* REST ONLY between UI and REST API
* Telemetry must originate in Redis
* No polling

Do NOT generate implementation code yet.

Only:

* architecture
* folder structure
* communication diagrams
* DTO/contracts design
* recommended implementation order
