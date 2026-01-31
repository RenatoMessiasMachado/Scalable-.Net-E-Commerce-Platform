# Arquitetura da Plataforma E-Commerce

## Visão Geral

Esta plataforma implementa uma arquitetura de microserviços completa para e-commerce, utilizando padrões modernos e melhores práticas da indústria.

## Diagrama de Arquitetura

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           CLIENTS (Web, Mobile, API)                     │
└──────────────────────────────┬──────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         API Gateway (NGINX)                              │
│                         Port: 8080                                       │
└──────────────────────────────┬──────────────────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                          MICROSERVICES LAYER                              │
├───────────────┬───────────────┬──────────────┬──────────────┬────────────┤
│               │               │              │              │            │
│  User         │  Product      │  Cart        │  Order       │  Payment   │
│  Service      │  Service      │  Service     │  Service     │  Service   │
│  :5001        │  :5002        │  :5003       │  :5004       │  :5005     │
│               │               │              │              │            │
│  ┌─────────┐  │  ┌─────────┐  │              │  ┌─────────┐ │            │
│  │PostgreSQL│  │  │PostgreSQL│  │              │  │PostgreSQL│ │            │
│  │  :5432  │  │  │  :5433  │  │              │  │  :5434  │ │            │
│  └─────────┘  │  └─────────┘  │              │  └─────────┘ │            │
│               │               │              │              │            │
│               │  ┌─────────┐  │  ┌────────┐  │              │            │
│               │  │ Redis   │──┼──│ Redis  │  │              │            │
│               │  │ Cache   │  │  │ Cart   │  │              │            │
│               │  └─────────┘  │  └────────┘  │              │            │
└───────┬───────┴───────┬───────┴──────┬───────┴──────┬───────┴────────────┘
        │               │              │              │
        │               │              │              │
        └───────────────┴──────────────┴──────────────┴───────────┐
                                                                   │
                                                                   ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      NOTIFICATION SERVICE                                │
│                           :5006                                          │
│                                                                          │
│                    ┌──────────┐    ┌──────────┐                        │
│                    │ SendGrid │    │  Twilio  │                        │
│                    │  Email   │    │   SMS    │                        │
│                    └──────────┘    └──────────┘                        │
└──────────────────────────────────────────────────────────────────────────┘
                                     ▲
                                     │
┌─────────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                                │
├────────────────┬─────────────────┬─────────────────┬────────────────────┤
│                │                 │                 │                    │
│  RabbitMQ      │  Consul         │  Elasticsearch  │  Redis             │
│  :5672, :15672 │  :8500          │  :9200          │  :6379             │
│  Message Broker│  Service        │  Log Storage    │  Cache & Session   │
│                │  Discovery      │                 │  Storage           │
│                │                 │                 │                    │
│                │                 │  Kibana         │                    │
│                │                 │  :5601          │                    │
│                │                 │  Log Viewer     │                    │
└────────────────┴─────────────────┴─────────────────┴────────────────────┘
```

## Padrões de Arquitetura Implementados

### 1. **Microservices Architecture**
- Cada serviço é independente e possui sua própria base de dados
- Comunicação via REST APIs e mensageria assíncrona
- Deploy e escala independentes

### 2. **API Gateway Pattern**
- Ponto único de entrada para clientes
- Roteamento de requisições
- Load balancing automático

### 3. **Event-Driven Architecture**
- Comunicação assíncrona via RabbitMQ
- Desacoplamento entre serviços
- Processamento eventual consistente

### 4. **Service Discovery**
- Registro automático de serviços com Consul
- Health checks contínuos
- Descoberta dinâmica de instâncias

### 5. **Database per Service**
- Cada microserviço possui seu próprio banco de dados
- Autonomia e isolamento de dados
- Sem dependências compartilhadas

### 6. **Caching Strategy**
- Redis para cache de produtos
- Cache de sessão para carrinho de compras
- Invalidação automática de cache

### 7. **Centralized Logging**
- ELK Stack (Elasticsearch, Logstash, Kibana)
- Agregação de logs de todos os serviços
- Rastreamento distribuído

## Fluxo de Dados

### Fluxo de Criação de Pedido

```
1. Cliente → API Gateway → User Service
   └─> Login / Autenticação
   
2. Cliente → API Gateway → Product Service
   └─> Listar produtos / Buscar produto
   
3. Cliente → API Gateway → Cart Service
   └─> Adicionar itens ao carrinho (Redis)
   
4. Cliente → API Gateway → Order Service
   └─> Criar pedido
   └─> Publicar OrderCreatedEvent (RabbitMQ)
   
5. Order Service → RabbitMQ → Payment Service
   └─> Processar pagamento (Stripe)
   └─> Publicar PaymentProcessedEvent
   
6. Payment Service → RabbitMQ → Order Service
   └─> Atualizar status do pedido
   
7. Order Service → RabbitMQ → Notification Service
   └─> Enviar confirmação por email (SendGrid)
   └─> Enviar SMS (Twilio)
```

## Eventos de Integração

### 1. UserRegisteredEvent
```json
{
  "id": "guid",
  "createdAt": "datetime",
  "userId": "guid",
  "email": "string",
  "fullName": "string"
}
```
**Publicado por:** User Service  
**Consumido por:** Notification Service

### 2. OrderCreatedEvent
```json
{
  "id": "guid",
  "createdAt": "datetime",
  "orderId": "guid",
  "userId": "guid",
  "totalAmount": "decimal",
  "userEmail": "string",
  "items": [...]
}
```
**Publicado por:** Order Service  
**Consumido por:** Payment Service, Notification Service

### 3. PaymentProcessedEvent
```json
{
  "id": "guid",
  "createdAt": "datetime",
  "orderId": "guid",
  "paymentId": "guid",
  "success": "boolean",
  "transactionId": "string"
}
```
**Publicado por:** Payment Service  
**Consumido por:** Order Service, Notification Service

### 4. OrderShippedEvent
```json
{
  "id": "guid",
  "createdAt": "datetime",
  "orderId": "guid",
  "trackingNumber": "string",
  "userEmail": "string"
}
```
**Publicado por:** Order Service  
**Consumido por:** Notification Service

### 5. InventoryUpdatedEvent
```json
{
  "id": "guid",
  "createdAt": "datetime",
  "productId": "guid",
  "newQuantity": "int"
}
```
**Publicado por:** Product Service  
**Consumido por:** (Futuro: Warehouse Service)

## Estratégias de Resiliência

### 1. Health Checks
Todos os serviços expõem endpoint `/health`:
```bash
GET /health
Response: 200 OK "healthy"
```

### 2. Circuit Breaker (Recomendado)
Implementar Polly para:
- Retry policies
- Circuit breaker
- Timeout policies
- Fallback strategies

### 3. Graceful Degradation
- Se Product Service falhar → Mostrar produtos em cache
- Se Payment Service falhar → Queue payment para processamento posterior
- Se Notification Service falhar → Retry com backoff exponencial

### 4. Data Consistency
- **Strong Consistency:** Dentro de cada microserviço
- **Eventual Consistency:** Entre microserviços (via eventos)
- **Saga Pattern:** Para transações distribuídas complexas

## Segurança

### 1. Autenticação
- JWT tokens com expiração de 24 horas
- Tokens assinados com HMAC-SHA256
- Claims incluem: userId, email, fullName

### 2. Autorização
- Validação de token em cada requisição
- Role-based access control (RBAC) - futuro
- API rate limiting no gateway

### 3. Proteção de Dados
- Senhas hashadas com BCrypt (salt rounds: 10)
- Dados sensíveis nunca em logs
- HTTPS em produção

### 4. API Security
- CORS configurado
- Input validation em todos os endpoints
- SQL injection prevention (EF Core parametrizado)

## Escalabilidade

### Escala Horizontal
```bash
# Escalar Product Service
docker-compose up -d --scale product-service=5

# Escalar Order Service
docker-compose up -d --scale order-service=3
```

### Load Balancing
NGINX distribui requisições automaticamente entre instâncias.

### Database Scaling
- **Read Replicas:** PostgreSQL com replicação
- **Sharding:** Por tenant ou região (futuro)
- **Connection Pooling:** EF Core gerencia automaticamente

### Cache Strategy
- **Cache Aside:** Produtos em Redis
- **Write Through:** Invalidação em updates
- **TTL:** 30 minutos para produtos

## Monitoramento

### 1. Logs
- **Serilog:** Logging estruturado
- **Elasticsearch:** Armazenamento
- **Kibana:** Visualização
- **Index Pattern:** `ecommerce-{service}-{date}`

### 2. Métricas (Recomendado)
- **Prometheus:** Coleta de métricas
- **Grafana:** Dashboards
- Métricas: Request rate, Error rate, Duration

### 3. Distributed Tracing (Recomendado)
- **Jaeger:** Rastreamento distribuído
- Correlation IDs em todos os eventos

### 4. Alerting (Recomendado)
- Alertas para:
  - Serviços offline
  - Alta taxa de erros (>5%)
  - Latência elevada (>2s)
  - Espaço em disco baixo

## Performance

### Otimizações Implementadas

1. **Caching:**
   - Produtos em cache (Redis)
   - Cache hit ratio esperado: >80%

2. **Database:**
   - Índices em colunas frequentemente consultadas
   - Connection pooling
   - Prepared statements

3. **Async Processing:**
   - Notificações processadas de forma assíncrona
   - Non-blocking I/O

4. **Compression:**
   - GZIP compression no API Gateway

### Benchmarks Esperados

- **Latência média:** <200ms
- **P95 latência:** <500ms
- **Throughput:** >1000 req/s (com escala)
- **Disponibilidade:** 99.9% uptime

## Deployment

### Development
```bash
make dev
```

### Production
```bash
# Docker Swarm
docker swarm init
docker stack deploy -c docker-compose.yml ecommerce

# Kubernetes
kubectl apply -f k8s/
```

### CI/CD Pipeline
```
Code Push → GitHub Actions
    ↓
Build & Test
    ↓
Docker Build & Push
    ↓
Deploy to Staging
    ↓
Automated Tests
    ↓
Deploy to Production
    ↓
Smoke Tests
```

## Disaster Recovery

### Backup Strategy
```bash
# Automated daily backups
make backup-db

# Backup storage
- Local: 7 days
- S3: 30 days
- Glacier: 1 year
```

### Recovery Time Objective (RTO)
- **Infrastructure:** <15 minutes
- **Data:** <1 hour
- **Full System:** <2 hours

### Recovery Point Objective (RPO)
- **Database:** <5 minutes (continuous backup)
- **Redis:** Acceptable data loss (cache)

## Custos Estimados (AWS)

### Minimum Production Setup
- **EC2 (t3.medium x 6):** $250/month
- **RDS PostgreSQL (3x db.t3.small):** $150/month
- **ElastiCache Redis:** $50/month
- **Application Load Balancer:** $25/month
- **Data Transfer:** $50/month
- **CloudWatch Logs:** $30/month
- **Total:** ~$555/month

### Optimized Production
- **ECS Fargate:** $400/month
- **RDS Aurora Serverless:** $200/month
- **ElastiCache:** $100/month
- **CloudFront CDN:** $50/month
- **Total:** ~$750/month

## Roadmap Técnico

### Fase 1 (Atual)
- ✅ Microserviços core
- ✅ API Gateway
- ✅ Service Discovery
- ✅ Event-driven messaging
- ✅ Centralized logging

### Fase 2 (Q1 2026)
- [ ] Circuit Breaker (Polly)
- [ ] Distributed Tracing (Jaeger)
- [ ] Metrics (Prometheus/Grafana)
- [ ] API Versioning
- [ ] Rate Limiting

### Fase 3 (Q2 2026)
- [ ] CQRS Pattern
- [ ] Event Sourcing
- [ ] GraphQL Gateway
- [ ] WebSockets (real-time updates)
- [ ] Frontend (React/Next.js)

### Fase 4 (Q3 2026)
- [ ] Multi-tenancy
- [ ] Kubernetes Helm Charts
- [ ] Service Mesh (Istio)
- [ ] Advanced Analytics
- [ ] Machine Learning recommendations

## Contribuindo

Consulte `CONTRIBUTING.md` para guidelines de desenvolvimento.

## Licença

MIT License - Consulte `LICENSE` para detalhes.
