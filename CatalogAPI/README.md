CatalogAPI - DDD-style microservice sample

This project implements the CatalogAPI microservice (games catalog) as described in the tech challenge. It includes:
- DDD structure with domain, application services, and infrastructure layers
- In-memory repositories for demo
- MassTransit + RabbitMQ integration for publishing `OrderPlacedEvent` and consuming `PaymentProcessedEvent`
- JWT authentication stub (replace key and validation for production)

How to run:
- Start RabbitMQ locally (default at localhost)
- dotnet run in the `CatalogAPI` project

Note: This is a minimal sample focused on messaging flow and structure. Replace in-memory stores and secrets before production.