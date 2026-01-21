# CatalogAPI - Microserviço de Catálogo

## ?? Quick Start

### Deploy no Kubernetes (COMPLETO)

```powershell
# 1. Navegar para a pasta do projeto
cd CatalogAPI/CatalogAPI

# 2. Build da imagem Docker
docker build -t catalogapi:latest .

# 3. Deploy no Kubernetes (ordem correta)
cd k8s

# SQL Server
kubectl apply -f sql-catalog-secret.yml
kubectl apply -f sql-catalog-deployment.yml
kubectl apply -f sql-catalog-service.yml

# CatalogAPI
kubectl apply -f configmap.yaml
kubectl apply -f secret.yaml
kubectl apply -f deployment.yaml

# 4. Verificar status
kubectl get pods
kubectl logs -f deployment/catalogapi

# 5. Testar API
kubectl port-forward service/catalogapi 8080:80
```

### ?? Deploy Automático

Use os scripts PowerShell criados:

```powershell
# Opção 1: Deploy completo (inclui SQL Server)
.\apply-all.ps1

# Opção 2: Rebuild + Redeploy (após mudanças no código)
.\rebuild-and-deploy.ps1

# Opção 3: Preparar conexão DBeaver
.\prepare-dbeaver.ps1
```

---

## ?? Endpoints

- **API**: http://localhost:8080/swagger
- **Health**: http://localhost:8080/health
- **Readiness**: http://localhost:8080/health/ready
- **Games API**: http://localhost:8080/api/games

---

## ?? Configuração

### Variáveis de Ambiente
O .NET Configuration usa `__` (double underscore) para hierarquia de seções:

```bash
# Configuração de Porta (CRÍTICO)
ASPNETCORE_URLS=http://+:8080
ASPNETCORE_ENVIRONMENT=Production

# RabbitMQ
RABBITMQ__Host=rabbitmq
RABBITMQ__VirtualHost=/
RABBITMQ__Username=fiap
RABBITMQ__Password=fiap123

# JWT
JWT__Key=very_secret_demo_key_please_change

# SQL Server
ConnectionStrings__CatalogDb=Server=sql-catalog,1433;Database=CatalogDb;User Id=sa;Password=StrongPassword!123;TrustServerCertificate=True;
```

### Mapeamento no Código
```csharp
// Program.cs lê assim:
builder.Configuration["RABBITMQ:Host"]              // ? RABBITMQ__Host
builder.Configuration["RABBITMQ:VirtualHost"]       // ? RABBITMQ__VirtualHost
builder.Configuration["ConnectionStrings:CatalogDb"] // ? ConnectionStrings__CatalogDb
builder.Configuration["JWT:Key"]                    // ? JWT__Key
```

---

## ?? Kubernetes

### Recursos Aplicados
```
# SQL Server
Secret:     sql-catalog-secret     (senha SA)
Deployment: sql-catalog            (SQL Server 2019)
Service:    sql-catalog            (ClusterIP, porta 1433)

# CatalogAPI
ConfigMap:  catalogapi-config      (variáveis não-sensíveis)
Secret:     catalogapi-secret      (credenciais + connection string)
Deployment: catalogapi             (1 réplica)
Service:    catalogapi             (ClusterIP, porta 80 ? 8080)
```

### Portas Configuradas
- **CatalogAPI Container**: 8080 (porta não-privilegiada)
- **CatalogAPI Service**: 80 (ClusterIP interno)
- **SQL Server Container**: 1433 (porta padrão)
- **Port-forward API**: `kubectl port-forward service/catalogapi 8080:80`
- **Port-forward SQL**: `kubectl port-forward service/sql-catalog 14332:1433`

> ?? Importante: O container roda como usuário não-root (`USER app`), portanto só pode usar portas ? 1024

---

## ??? Banco de Dados (SQL Server)

### Configuração
- Servidor: SQL Server 2019 (container)
- ORM: Entity Framework Core 8.0
- Migrations: Aplicadas automaticamente no startup
- Dados: Persistidos em `emptyDir` (perdidos ao deletar pod)

### Tabelas Criadas
1. **Games** - Catálogo de jogos
   - Id (uniqueidentifier)
   - Title (nvarchar 200)
   - Description (nvarchar 2000)
   - Price (decimal 18,2)

2. **UserLibraries** - Biblioteca de jogos dos usuários
   - Id (uniqueidentifier)
   - UserId (uniqueidentifier)
   - GameId (uniqueidentifier)
   - CreatedAt (datetime2)

3. **OrderGames** - Pedidos de jogos
   - Id (uniqueidentifier)
   - UserId (uniqueidentifier)
   - GameId (uniqueidentifier)
   - IsProcessed (bit)

4. **__EFMigrationsHistory** - Controle de migrations

### Conexão DBeaver

#### Port-forward:
```powershell
kubectl port-forward service/sql-catalog 14332:1433
```

#### Configuração:
| Campo | Valor |
|-------|-------|
| Database | Microsoft SQL Server |
| Host | localhost |
| Port | 14332 |
| Database | CatalogDb |
| Username | sa |
| Password | StrongPassword!123 |
| Driver Property | trustServerCertificate=true |
| Driver Property | encrypt=false |

#### Queries de Teste:
```sql
-- Ver migrations aplicadas
SELECT * FROM __EFMigrationsHistory;

-- Ver todas as tabelas
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';

-- Ver jogos (2 de seed)
SELECT * FROM Games;

-- Ver biblioteca de usuários
SELECT * FROM UserLibraries;
```

---

## ?? RabbitMQ Integration

### Consumidor Configurado
- Queue: catalog-payment-processed
- Consumer: PaymentProcessedConsumer
- Event: PaymentProcessedEvent

### Testar Integração com RabbitMQ

#### 1. Acessar RabbitMQ Management UI
```sh
kubectl port-forward service/rabbitmq 15672:15672
# Acesse: http://localhost:15672
# Login: fiap / fiap123
```

#### 2. Publicar Mensagem de Teste
Na UI do RabbitMQ:
1. Vá em Queues ? catalog-payment-processed
2. Clique em Publish message
3. Configure:
   - Content type: application/vnd.masstransit+json
   - Delivery mode: 2 (persistent)
4. Cole o payload:

```json
{
  "messageId": "00000000-0000-0000-0000-000000000001",
  "conversationId": "00000000-0000-0000-0000-000000000002",
  "sourceAddress": "rabbitmq://rabbitmq/payment_api",
  "destinationAddress": "rabbitmq://rabbitmq/catalog-payment-processed",
  "messageType": [
    "urn:message:Shared.Events:PaymentProcessedEvent"
  ],
  "message": {
    "orderId": "550e8400-e29b-41d4-a716-446655440001",
    "userId": "550e8400-e29b-41d4-a716-446655440002",
    "gameId": "550e8400-e29b-41d4-a716-446655440003",
    "price": 299.90,
    "status": "Approved"
  },
  "sentTime": "2026-01-20T19:00:00Z"
}
```

#### 3. Verificar Logs
```sh
kubectl logs -f deployment/catalogapi
```

---

## ?? Testes

### Testar API (Port-forward ativo)

```bash
# Health Check
curl http://localhost:8080/health

# Listar jogos
curl http://localhost:8080/api/games

# Verificar conectividade com RabbitMQ
curl http://localhost:8080/health/ready
```

### Testes Completos
Para mais informações sobre testes:
- Payloads de exemplo para API REST
- Autenticação JWT (simplificada para demo)
- Integração com RabbitMQ
- Health checks disponíveis

---

## ?? Estrutura

```
CatalogAPI/
??? Controllers/          # API endpoints
?   ??? GamesController.cs
?   ??? HealthController.cs
??? Application/          # Services
?   ??? Services/
?       ??? GameService.cs
??? Domain/               # Entities & Events
?   ??? Entities/
?   ?   ??? Game.cs
?   ?   ??? UserLibraryEntry.cs
?   ?   ??? OrderGame.cs
?   ??? Events/
?       ??? OrderPlacedEvent.cs
??? Infrastructure/       # Repositories, Consumers, Persistence
?   ??? Persistence/
?   ?   ??? CatalogDbContext.cs
?   ??? Repositories/
?   ?   ??? GameRepository.cs
?   ?   ??? UserLibraryRepository.cs
?   ?   ??? OrderGameRepository.cs
?   ??? Consumers/
?       ??? PaymentProcessedConsumer.cs
??? Migrations/           # EF Core Migrations
?   ??? 20260118234430_Initial.cs
??? k8s/                  # Kubernetes manifests (organizado por app)
?   ??? namespace.yaml
?   ??? services/                  # Services/Ingress/etc.
?   ?   ??? rabbitmq-service.yaml
?   ?   ??? sql-catalog-service.yaml
?   ?   ??? catalogapi-service.yaml
?   ??? catalog-api/               # Manifests do CatalogAPI
?   ?   ??? deployment.yaml
?   ?   ??? configmap.yaml
?   ?   ??? secret.yaml
?   ?   ??? hpa.yaml               # (se aplicável)
?   ??? sql-catalog/               # Manifests do SQL Server
?   ?   ??? deployment.yaml
?   ?   ??? secret.yaml
?   ?   ??? pvc.yaml               # (se aplicável)
?   ??? apply-all.ps1              # Script de deploy completo
?   ??? rebuild-and-deploy.ps1     # Script de rebuild
?   ??? prepare-dbeaver.ps1        # Script de preparação DBeaver
??? Dockerfile             # Multi-stage build otimizado
??? Program.cs             # Configuração da aplicação
??? README.md
```

---

## ?? Troubleshooting

### Pod não inicia (CrashLoopBackOff)
```bash
# Ver logs detalhados
kubectl logs -f deployment/catalogapi

# Verificar eventos
kubectl describe pod -l app=catalogapi

# Problemas comuns:
# 1. Imagem não encontrada ? imagePullPolicy: IfNotPresent
# 2. RabbitMQ não conecta ? Verifique: kubectl get pods | grep rabbitmq
# 3. SQL Server não conecta ? Verifique: kubectl get pods | grep sql-catalog
```

### Erro: ErrImageNeverPull
? RESOLVIDO: O deployment agora usa imagePullPolicy: IfNotPresent

**Como corrigir:**
```powershell
# Rebuild da imagem
docker build -t catalogapi:latest .

# Aplicar deployment atualizado
kubectl apply -f k8s/deployment.yaml
kubectl delete pod -l app=catalogapi
```

### Erro: Cannot open database "CatalogDb"
? RESOLVIDO: Crie o banco e reinicie o pod

**Causa**: Banco não foi criado ou migrations não rodaram

**Solução:**
```powershell
# Criar banco manualmente
kubectl exec sql-catalog-pod-name -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "StrongPassword!123" -C -Q "CREATE DATABASE CatalogDb;"

# Reiniciar pod do CatalogAPI para rodar migrations
kubectl delete pod -l app=catalogapi
```

### Erro: Permission denied (porta 80)
? RESOLVIDO: O container usa porta 8080

- ASPNETCORE_URLS=http://+:8080
- Container expõe porta 8080
- Service mapeia 80 ? 8080

### Erro ao consumir mensagem do RabbitMQ
```
MT-Fault-Message: Value cannot be null. (Parameter 'envelope')
```

? RESOLVIDO: Use o formato correto do MassTransit (veja seção RabbitMQ Integration)

### Migrations não rodaram
```powershell
# Verificar logs
kubectl logs deployment/catalogapi | Select-String "migration|CREATE TABLE"

# Forçar execução das migrations
kubectl delete pod -l app=catalogapi

# Verificar tabelas criadas
kubectl exec sql-catalog-pod-name -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "StrongPassword!123" -C -Q "USE CatalogDb; SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;"
```

### Verificar Status Geral
```bash
# Status de todos os pods
kubectl get pods

# Logs CatalogAPI
kubectl logs -f deployment/catalogapi

# Logs SQL Server
kubectl logs -f deployment/sql-catalog

# Remover tudo e redeployar
kubectl delete -f k8s/
.\apply-all.ps1
```

---

## ?? Segurança

- ? Container roda como usuário não-root (`USER app`)
- ? Secrets separados do código
- ? Resource limits configurados (128Mi-512Mi RAM, 100m-500m CPU)
- ?? JWT key é demo - MUDE EM PRODUÇÃO
- ?? Senha SQL Server é demo - MUDE EM PRODUÇÃO
- ?? trustServerCertificate=true - Use certificados válidos em produção

---

## ?? Notas Técnicas

### Armazenamento de Dados
- ?? SQL Server: Dados armazenados em SQL Server 2019
- ?? emptyDir: Volume temporário (dados perdidos ao deletar pod)
- ?? Demo: Para produção, use PersistentVolumeClaim (PVC)

### Entity Framework Core
- ?? ORM: Entity Framework Core 8.0
- ?? Migrations: Aplicadas automaticamente no startup
- ??? Contexto: CatalogDbContext
- ??? Code First: Modelagem de dados no código C#

### MassTransit + RabbitMQ
- ?? Usa MassTransit 8.0 para abstração do RabbitMQ
- ?? Consumer: PaymentProcessedConsumer
- ?? Queue: catalog-payment-processed
- ?? Publica: OrderPlacedEvent

### Autenticação
- ?? JWT Bearer Token
- ?? Simplificada: Não valida issuer/audience (apenas demo)
- ?? Secret: very_secret_demo_key_please_change

---

## ?? Melhorias para Produção

### Banco de Dados
```yaml
# Use PersistentVolumeClaim
volumes:
  - name: sql-data
    persistentVolumeClaim:
      claimName: sql-catalog-pvc
```

### Health Checks
Habilite health checks no deployment.yaml:
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 5
```

### Secrets Management
- Use Azure Key Vault ou HashiCorp Vault
- Rotação automática de senhas
- Certificados válidos para SQL Server

### Observabilidade
- Adicione Application Insights
- Configure logs estruturados
- Implemente tracing distribuído