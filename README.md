# E-commerce Microservices Platform - .NET Core

Uma plataforma e-commerce escalável construída com arquitetura de microserviços usando .NET Core 8, Docker e tecnologias modernas.

## 🏗️ Arquitetura

### Microserviços

1. **User Service** (Porta 5001)
   - Autenticação e autorização com JWT
   - Gerenciamento de perfil de usuário
   - PostgreSQL para persistência
   - Publica eventos de registro de usuário

2. **Product Catalog Service** (Porta 5002)
   - Gerenciamento de catálogo de produtos
   - Pesquisa e filtros de produtos
   - Cache Redis para melhor performance
   - PostgreSQL para persistência

3. **Shopping Cart Service** (Porta 5003)
   - Gerenciamento de carrinho de compras
   - Armazenamento em Redis (session-based)
   - Adicionar/remover/atualizar itens

4. **Order Service** (Porta 5004)
   - Processamento de pedidos
   - Histórico de pedidos
   - Rastreamento de status
   - PostgreSQL para persistência
   - Publica eventos de criação de pedido

5. **Payment Service** (Porta 5005)
   - Integração com Stripe
   - Processamento de pagamentos
   - Publica eventos de pagamento processado

6. **Notification Service** (Porta 5006)
   - Envio de e-mails (SendGrid)
   - Envio de SMS (Twilio)
   - Consome eventos de outros serviços

### Componentes de Infraestrutura

- **API Gateway** (NGINX) - Porta 8080
  - Ponto de entrada único para clientes
  - Roteamento para microserviços
  - Load balancing

- **Service Discovery** (Consul) - Porta 8500
  - Registro automático de serviços
  - Health checks
  - Service discovery dinâmico

- **Message Broker** (RabbitMQ) - Porta 5672, 15672
  - Comunicação assíncrona entre serviços
  - Event-driven architecture
  - Management UI disponível

- **Cache** (Redis) - Porta 6379
  - Cache de produtos
  - Armazenamento de carrinho de compras
  - Session management

- **Centralized Logging** (ELK Stack)
  - Elasticsearch - Porta 9200
  - Kibana - Porta 5601
  - Agregação de logs de todos os serviços

- **Databases** (PostgreSQL)
  - User Database - Porta 5432
  - Product Database - Porta 5433
  - Order Database - Porta 5434

## 🚀 Começando

### Pré-requisitos

- Docker 20.10+
- Docker Compose 2.0+
- .NET 8 SDK (para desenvolvimento local)
- Git

### Configuração Inicial

1. **Clone o repositório:**
```bash
git clone <repository-url>
cd ecommerce-platform
```

2. **Configure as variáveis de ambiente:**

Edite o `docker-compose.yml` e atualize:
- Stripe API keys (PaymentService)
- SendGrid API key (NotificationService)
- Twilio credentials (NotificationService)

3. **Construa e inicie os serviços:**
```bash
docker-compose build
docker-compose up -d
```

4. **Verifique os serviços:**
```bash
docker-compose ps
```

### Acessando os Serviços

- **API Gateway**: http://localhost:8080
- **User Service**: http://localhost:5001/swagger
- **Product Service**: http://localhost:5002/swagger
- **Cart Service**: http://localhost:5003/swagger
- **Order Service**: http://localhost:5004/swagger
- **Payment Service**: http://localhost:5005/swagger
- **Notification Service**: http://localhost:5006/swagger
- **Consul UI**: http://localhost:8500
- **RabbitMQ Management**: http://localhost:15672 (admin/admin123)
- **Kibana**: http://localhost:5601

## 📋 Exemplos de API

### 1. Registrar Usuário

```bash
curl -X POST http://localhost:8080/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "John Doe",
    "email": "john@example.com",
    "password": "password123",
    "phoneNumber": "+5511999999999",
    "address": "Rua Example, 123"
  }'
```

### 2. Login

```bash
curl -X POST http://localhost:8080/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "password123"
  }'
```

**Resposta:**
```json
{
  "userId": "guid",
  "email": "john@example.com",
  "fullName": "John Doe",
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

### 3. Listar Produtos

```bash
curl http://localhost:8080/api/products
```

### 4. Buscar Produto Específico

```bash
curl http://localhost:8080/api/products/{productId}
```

### 5. Adicionar ao Carrinho

```bash
curl -X POST http://localhost:8080/api/cart/{userId}/items \
  -H "Content-Type: application/json" \
  -d '{
    "productId": "guid",
    "productName": "Laptop Dell XPS 15",
    "quantity": 1,
    "price": 1499.99
  }'
```

### 6. Ver Carrinho

```bash
curl http://localhost:8080/api/cart/{userId}
```

### 7. Criar Pedido

```bash
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "userId": "guid",
    "items": [
      {
        "productId": "guid",
        "productName": "Laptop Dell XPS 15",
        "quantity": 1,
        "price": 1499.99
      }
    ],
    "totalAmount": 1499.99,
    "shippingAddress": "Rua Example, 123"
  }'
```

## 🔄 Fluxo de Eventos

### Criação de Pedido
```
1. User Service → UserRegisteredEvent
2. Order Service → OrderCreatedEvent
3. Payment Service → PaymentProcessedEvent
4. Order Service → OrderShippedEvent
5. Notification Service → Consome todos os eventos e envia notificações
```

## 📊 Monitoramento e Logs

### Visualizar Logs no Kibana

1. Acesse http://localhost:5601
2. Configure o index pattern: `ecommerce-*`
3. Explore os logs de todos os microserviços

### Verificar Health Checks

Cada serviço expõe um endpoint `/health`:

```bash
curl http://localhost:5001/health  # User Service
curl http://localhost:5002/health  # Product Service
# ... etc
```

### Consul Service Discovery

Visite http://localhost:8500 para ver:
- Serviços registrados
- Status de saúde
- Configurações

## 🛠️ Desenvolvimento

### Executar Localmente (sem Docker)

1. **Inicie os serviços de infraestrutura:**
```bash
docker-compose up -d userdb productdb orderdb redis rabbitmq consul elasticsearch kibana
```

2. **Configure as connection strings:**
```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=userdb;Username=postgres;Password=postgres123"
```

3. **Execute um serviço:**
```bash
cd src/UserService
dotnet run
```

### Adicionar um Novo Microserviço

1. Crie uma nova pasta em `src/`
2. Adicione referência ao projeto Shared
3. Implemente health checks
4. Configure Consul registration
5. Adicione ao `docker-compose.yml`
6. Atualize o API Gateway

## 🔐 Segurança

- **JWT Authentication**: Tokens com expiração de 24 horas
- **Password Hashing**: BCrypt para senhas
- **HTTPS**: Configure certificados para produção
- **API Rate Limiting**: Configure no API Gateway
- **Input Validation**: Data annotations em todos os models

## 📈 Escalabilidade

### Escalar Horizontalmente

```bash
# Escalar Product Service para 3 instâncias
docker-compose up -d --scale product-service=3
```

### Load Balancing

O NGINX API Gateway automaticamente distribui requisições entre instâncias.

### Cache Strategy

- **Product Data**: Cache Redis com TTL de 30 minutos
- **Shopping Cart**: Redis com TTL de 7 dias
- **Cache Invalidation**: Automático nas atualizações

## 🧪 Testes

### Testes de Integração

```bash
cd tests
dotnet test
```

### Teste de Carga

Use ferramentas como Apache JMeter ou k6:

```bash
k6 run load-test.js
```

## 📦 Deploy para Produção

### Docker Swarm

```bash
docker swarm init
docker stack deploy -c docker-compose.yml ecommerce
```

### Kubernetes

```bash
kubectl apply -f k8s/
```

### CI/CD Pipeline

Exemplo com GitHub Actions:

```yaml
name: CI/CD Pipeline
on: [push]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Build Images
        run: docker-compose build
      - name: Run Tests
        run: docker-compose run --rm tests
      - name: Deploy
        run: ./deploy.sh
```

## 📝 Estrutura de Pastas

```
ecommerce-platform/
├── src/
│   ├── Shared/                 # Código compartilhado
│   │   ├── Events/            # Eventos de integração
│   │   ├── Messaging/         # RabbitMQ message bus
│   │   └── ServiceDiscovery/  # Consul integration
│   ├── UserService/
│   ├── ProductService/
│   ├── CartService/
│   ├── OrderService/
│   ├── PaymentService/
│   ├── NotificationService/
│   └── ApiGateway/
├── tests/
├── docker-compose.yml
└── README.md
```

## 🤝 Contribuindo

1. Fork o projeto
2. Crie uma feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está sob a licença MIT.

## 🙏 Agradecimentos

- .NET Core Team
- Docker Community
- RabbitMQ
- Consul by HashiCorp
- Elastic Stack

## 📞 Suporte

Para questões e suporte:
- Abra uma issue no GitHub
- Email: renato19mm@gmail.com
