# CatalogAPI

Microserviço responsável pelo catálogo de jogos e pela orquestração do fluxo de compra.

## Visão geral
- Expõe CRUD de `Game` via HTTP (Swagger em desenvolvimento).
- Publica `OrderPlacedEvent` via RabbitMQ (MassTransit) ao realizar pedido.
- Consome `PaymentProcessedEvent` para, quando aprovado, adicionar o jogo à biblioteca do usuário.
- Persistência com EF Core + SQL Server.
- Endpoints de saúde: `GET /health` e `GET /health/ready`.
- JWT habilitado (apenas para demo) para operações que modificam estado.

## Arquitetura
- `Domain/Entities`:
  - `Game`: `Id`, `Title`, `Description`, `Price`.
  - `UserLibraryEntry`: vincula `UserId` a `GameId`.
- `Domain/Events`:
  - `OrderPlacedEvent`: dispara pedido com `UserId`, `GameId`, `Price`.
  - `PaymentProcessedEvent`: resultado do pagamento com `Status` (`Approved`/`Rejected`).
- `Application`:
  - `GameService`: regras de negócio do catálogo e publicação/consumo de eventos.
- `Infrastructure/Persistence`:
  - `CatalogDbContext`: mapeamento EF Core.
- `Infrastructure/Repositories`:
  - `IGameRepository` -> `EfGameRepository`.
  - `IUserLibraryRepository` -> `EfUserLibraryRepository`.
- `Infrastructure/Consumers`:
  - `PaymentProcessedConsumer`: consome `PaymentProcessedEvent`.
- `Controllers`:
  - `GamesController`, `HealthController`.
- `Program.cs`: DI, MassTransit/RabbitMQ, JWT, EF Core e Swagger.

## Fluxo principal
1. O serviço inicia, aplica migrations e conecta ao RabbitMQ.
2. `GamesController` expõe operações de catálogo (CRUD) e `POST /api/games/{id}/order`.
3. Ao ordenar um jogo:
   - `GameService.PlaceOrderAsync` publica `OrderPlacedEvent`.
4. Quando o pagamento é processado pelo serviço externo:
   - `PaymentProcessedConsumer` recebe `PaymentProcessedEvent`.
   - Se `Approved`, `GameService` adiciona o jogo na biblioteca do usuário.

## Integração com RabbitMQ (MassTransit)
- Configurações via `RABBITMQ:Host`, `RABBITMQ:VirtualHost`, `RABBITMQ:Username`, `RABBITMQ:Password`.
- Fila de consumo: `payment_processed_queue`.

## Persistência (EF Core + SQL Server)
- Connection string via `ConnectionStrings:CatalogDb`.
- Exemplo (docker do usuário): `Server=localhost,14332;Database=CatalogDb;User Id=sa;Password=StrongPassword!123;TrustServerCertificate=True;`.
- Ao iniciar, o app aplica `Migrate()` e faz seed de jogos demo quando a tabela estiver vazia.

## Endpoints HTTP
- `GET /api/games`
- `GET /api/games/{id}`
- `POST /api/games` (requer JWT)
- `PUT /api/games/{id}` (requer JWT)
- `DELETE /api/games/{id}` (requer JWT)
- `POST /api/games/{id}/order?userId=<guid>` (requer JWT)
- `GET /health`, `GET /health/ready`

## Configuração
- Variáveis/`appsettings`:
  - `RABBITMQ:Host`, `RABBITMQ:VirtualHost`, `RABBITMQ:Username`, `RABBITMQ:Password`.
  - `ConnectionStrings:CatalogDb`.
  - `JWT:Key` (somente demo).
- Em desenvolvimento via `launchSettings.json`:
  - `RABBITMQ__HOST=localhost`, `RABBITMQ__VIRTUALHOST=/`, `RABBITMQ__USERNAME=guest`, `RABBITMQ__PASSWORD=guest`.
  - `ConnectionStrings__CatalogDb=Server=localhost,14332;Database=CatalogDb;User Id=sa;Password=StrongPassword!123;TrustServerCertificate=True;`.

## Mensagens RabbitMQ (exemplos)
- Evento publicado pelo CatalogAPI:
  - `OrderPlacedEvent` (exchange padrão do bus):
    ```json
    {
      "userId": "11111111-1111-1111-1111-111111111111",
      "gameId": "22222222-2222-2222-2222-222222222222",
      "price": 49.99
    }
    ```
- Evento consumido pelo CatalogAPI na fila `payment_processed_queue`:
  - `PaymentProcessedEvent`:
    ```json
    {
      "userId": "11111111-1111-1111-1111-111111111111",
      "gameId": "22222222-2222-2222-2222-222222222222",
      "price": 49.99,
      "status": "Approved"
    }
    ```

## Executando localmente
- Pré-requisitos: .NET 8 SDK, SQL Server disponível (ex.: container mssql 2019) e RabbitMQ
- Build: `dotnet build`
- Run: `dotnet run` no projeto `CatalogAPI`.
- Swagger em `/swagger`.

## Tecnologias
- .NET 8, C# 12
- EF Core (SQL Server)
- MassTransit
- RabbitMQ
- Swagger/OpenAPI
