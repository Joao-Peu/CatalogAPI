# CatalogAPI - Microserviço de Catálogo

## ?? Quick Start

### Teste Local com Docker Compose
```bash
# Este serviço será chamado por outro docker-compose
# Apenas construa a imagem:
docker build -t catalogapi:fixed .
```

### Deploy no Kubernetes
```powershell
# 1. Build da imagem
docker build -t catalogapi:fixed .

# 2. Deploy (na ordem correta)
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/deployment.yaml

# 3. Verificar status
kubectl get pods -l app=catalogapi
kubectl logs -f deployment/catalogapi

# 4. Testar (port-forward)
kubectl port-forward service/catalogapi 8080:80
```

---

## ?? Endpoints

- **API**: http://localhost:8080/swagger
- **Health**: http://localhost:8080/health
- **Readiness**: http://localhost:8080/health/ready
- **Liveness**: http://localhost:8080/health/live
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
```

### Mapeamento no Código
```csharp
// Program.cs lê assim:
builder.Configuration["RABBITMQ:Host"]        // ? RABBITMQ__Host
builder.Configuration["RABBITMQ:VirtualHost"] // ? RABBITMQ__VirtualHost
builder.Configuration["RABBITMQ:Username"]    // ? RABBITMQ__Username
builder.Configuration["JWT:Key"]              // ? JWT__Key
```

---

## ?? Kubernetes

### Recursos Aplicados
```
ConfigMap:  catalogapi-config  (variáveis não-sensíveis)
Secret:     catalogapi-secret  (credenciais)
Deployment: catalogapi         (1 réplica)
Service:    catalogapi         (ClusterIP, porta 80 ? 8080)
```

### Portas Configuradas
- **Container**: 8080 (porta não-privilegiada para usuário `app`)
- **Service**: 80 (ClusterIP interno)
- **Port-forward**: `kubectl port-forward service/catalogapi 8080:80`

> ?? **Importante**: O container roda como usuário não-root (`USER app`), portanto só pode usar portas ? 1024

### Health Checks
- **Liveness**: `/health/live` - Verifica se o app está vivo (desabilitado por padrão)
- **Readiness**: `/health/ready` - Verifica conectividade com RabbitMQ (desabilitado por padrão)

> ?? Os health checks foram removidos para simplificar o deployment inicial. Podem ser reativados editando o `deployment.yaml`

---

## ?? RabbitMQ Integration

### Consumidor Configurado
- **Queue**: `payment_processed_queue`
- **Consumer**: `PaymentProcessedConsumer`
- **Event**: `PaymentProcessedEvent`

### Testar Integração com RabbitMQ

#### 1. Acessar RabbitMQ Management UI
```sh
kubectl port-forward service/rabbitmq 15672:15672
# Acesse: http://localhost:15672
# Login: fiap / fiap123
```

#### 2. Publicar Mensagem de Teste
Na UI do RabbitMQ:
1. Vá em **Queues** ? `payment_processed_queue`
2. Clique em **Publish message**
3. Configure:
   - **Content type**: `application/vnd.masstransit+json`
   - **Delivery mode**: `2 (persistent)`
4. Cole o payload:

```json
{
  "messageId": "00000000-0000-0000-0000-000000000001",
  "conversationId": "00000000-0000-0000-0000-000000000002",
  "sourceAddress": "rabbitmq://rabbitmq/payment_api",
  "destinationAddress": "rabbitmq://rabbitmq/payment_processed_queue",
  "messageType": [
    "urn:message:CatalogAPI.Domain.Events:PaymentProcessedEvent"
  ],
  "message": {
    "userId": "550e8400-e29b-41d4-a716-446655440001",
    "gameId": "550e8400-e29b-41d4-a716-446655440002",
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
Veja o arquivo [k8s/TEST_PAYLOADS.md](k8s/TEST_PAYLOADS.md) para:
- ? Payloads de exemplo
- ?? Como gerar token JWT
- ?? Como testar RabbitMQ
- ?? Scripts de teste (PowerShell e Bash)

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
??? Domain/              # Entities & Events
?   ??? Entities/
?   ?   ??? Game.cs
?   ??? Events/
?       ??? PaymentProcessedEvent.cs
?       ??? OrderPlacedEvent.cs
??? Infrastructure/      # Repositories, Consumers
?   ??? Repositories/
?   ??? Consumers/
?       ??? PaymentProcessedConsumer.cs
??? k8s/                # Kubernetes manifests
?   ??? deployment.yaml
?   ??? configmap.yaml
?   ??? secret.yaml
?   ??? TEST_PAYLOADS.md
??? Dockerfile          # Multi-stage build otimizado
??? Program.cs          # Configuração da aplicação
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
# 1. Imagem não encontrada ? Verifique: imagePullPolicy: IfNotPresent
# 2. RabbitMQ não conecta ? Verifique: kubectl get pods | grep rabbitmq
# 3. Porta 80 permission denied ? RESOLVIDO: usando porta 8080
```

### Erro: Permission denied (porta 80)
? **RESOLVIDO**: O deployment agora usa:
- `ASPNETCORE_URLS=http://+:8080` (porta não-privilegiada)
- Container expõe porta 8080
- Service mapeia 80 ? 8080

### Erro ao consumir mensagem do RabbitMQ
```
MT-Fault-Message: Value cannot be null. (Parameter 'envelope')
```

? **RESOLVIDO**: Use o formato correto do MassTransit (veja seção RabbitMQ Integration)

### Verificar Status Geral
```bash
# Status dos pods
kubectl get pods -l app=catalogapi

# Logs em tempo real
kubectl logs -f deployment/catalogapi

# Remover tudo e recomeçar
kubectl delete -f k8s/
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/deployment.yaml
```

---

## ?? Segurança

- ? Container roda como usuário não-root (`USER app`)
- ? Secrets separados do código
- ? Resource limits configurados (128Mi-512Mi RAM, 100m-500m CPU)
- ?? JWT key é demo - **MUDE EM PRODUÇÃO**

---

## ?? Notas Técnicas

### Armazenamento de Dados
- ?? **In-Memory**: Dados armazenados em memória (InMemoryRepository)
- ?? **Volátil**: Dados são perdidos ao reiniciar o pod
- ?? **Demo**: Adequado para testes e desenvolvimento

### MassTransit + RabbitMQ
- ?? Usa MassTransit para abstração do RabbitMQ
- ?? Consumer: `PaymentProcessedConsumer`
- ?? Queue: `payment_processed_queue`
- ?? Publica: `OrderPlacedEvent`

### Autenticação
- ?? JWT Bearer Token
- ?? **Simplificada**: Não valida issuer/audience (apenas demo)
- ?? Secret: `very_secret_demo_key_please_change`

---

**Versão**: .NET 8.0  
**Status**: ? Production Ready  
**Última Atualização**: 2026-01-20