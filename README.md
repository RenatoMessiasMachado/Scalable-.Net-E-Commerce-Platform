# E-commerce Microservices Platform - .NET Core

A scalable e-commerce platform built with microservices architecture using .NET Core 8, Docker, and modern technologies.

## 🏗️ Architecture

### Microservices

1. **User Service** (Port 5001)
   - JWT authentication and authorization
   - User profile management
   - PostgreSQL persistence
   - Publishes user registration events

2. **Product Catalog Service** (Port 5002)
   - Product catalog management
   - Product search and filtering
   - Redis cache for improved performance
   - PostgreSQL persistence

3. **Shopping Cart Service** (Port 5003)
   - Shopping cart management
   - Redis-based storage (session-based)
   - Add/remove/update items

4. **Order Service** (Port 5004)
   - Order processing
   - Order history
   - Status tracking
   - PostgreSQL persistence
   - Publishes order creation events

5. **Payment Service** (Port 5005)
   - Stripe integration
   - Payment processing
   - Publishes payment processed events

6. **Notification Service** (Port 5006)
   - Email delivery (SendGrid)
   - SMS delivery (Twilio)
   - Consumes events from other services

### Infrastructure Components

- **API Gateway** (NGINX) - Port 8080
  - Single entry point for clients
  - Routing to microservices
  - Load balancing

- **Service Discovery** (Consul) - Port 8500
  - Automatic service registration
  - Health checks
  - Dynamic service discovery

- **Message Broker** (RabbitMQ) - Ports 5672, 15672
  - Asynchronous communication between services
  - Event-driven architecture
  - Management UI available

- **Cache** (Redis) - Port 6379
  - Product caching
  - Shopping cart storage
  - Session management

- **Centralized Logging** (ELK Stack)
  - Elasticsearch - Port 9200
  - Kibana - Port 5601
  - Log aggregation from all services

- **Databases** (PostgreSQL)
  - User Database - Port 5432
  - Product Database - Port 5433
  - Order Database - Port 5434

## 🚀 Getting Started

### Prerequisites

- Docker 20.10+
- Docker Compose 2.0+
- .NET 8 SDK (for local development)
- Git

### Initial Setup

1. **Clone the repository:**
```bash
git clone 
cd ecommerce-platform
```

2. **Configure environment variables:**

Edit `docker-compose.yml` and update:
- Stripe API keys (PaymentService)
- SendGrid API key (NotificationService)
- Twilio credentials (NotificationService)

3. **Build and start services:**
```bash
docker-compose build
docker-compose up -d
```

4. **Verify services:**
```bash
docker-compose ps
```

### Accessing Services

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

## 📋 API Examples

### 1. Register User

```bash
curl -X POST http://localhost:8080/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "John Doe",
    "email": "john@example.com",
    "password": "password123",
    "phoneNumber": "+5511999999999",
    "address": "123 Main Street"
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

**Response:**
```json
{
  "userId": "guid",
  "email": "john@example.com",
  "fullName": "John Doe",
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

### 3. List Products

```bash
curl http://localhost:8080/api/products
```

### 4. Get Specific Product

```bash
curl http://localhost:8080/api/products/{productId}
```

### 5. Add to Cart

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

### 6. View Cart

```bash
curl http://localhost:8080/api/cart/{userId}
```

### 7. Create Order

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
    "shippingAddress": "123 Main Street"
  }'
```

## 🔄 Event Flow

### Order Creation Flow
```
1. User Service → UserRegisteredEvent
2. Order Service → OrderCreatedEvent
3. Payment Service → PaymentProcessedEvent
4. Order Service → OrderShippedEvent
5. Notification Service → Consumes all events and sends notifications
```

## 📊 Monitoring and Logging

### View Logs in Kibana

1. Access http://localhost:5601
2. Configure index pattern: `ecommerce-*`
3. Explore logs from all microservices

### Check Health Status

Each service exposes a `/health` endpoint:

```bash
curl http://localhost:5001/health  # User Service
curl http://localhost:5002/health  # Product Service
# ... etc
```

### Consul Service Discovery

Visit http://localhost:8500 to view:
- Registered services
- Health status
- Configuration

## 🛠️ Development

### Running Locally (without Docker)

1. **Start infrastructure services:**
```bash
docker-compose up -d userdb productdb orderdb redis rabbitmq consul elasticsearch kibana
```

2. **Configure connection strings:**
```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=userdb;Username=postgres;Password=postgres123"
```

3. **Run a service:**
```bash
cd src/UserService
dotnet run
```

### Adding a New Microservice

1. Create a new folder in `src/`
2. Add reference to the Shared project
3. Implement health checks
4. Configure Consul registration
5. Add to `docker-compose.yml`
6. Update the API Gateway

## 🔐 Security

- **JWT Authentication**: 24-hour token expiration
- **Password Hashing**: BCrypt for passwords
- **HTTPS**: Configure certificates for production
- **API Rate Limiting**: Configure in API Gateway
- **Input Validation**: Data annotations on all models

## 📈 Scalability

### Horizontal Scaling

```bash
# Scale Product Service to 3 instances
docker-compose up -d --scale product-service=3
```

### Load Balancing

The NGINX API Gateway automatically distributes requests across instances.

### Cache Strategy

- **Product Data**: Redis cache with 30-minute TTL
- **Shopping Cart**: Redis with 7-day TTL
- **Cache Invalidation**: Automatic on updates

## 🧪 Testing

### Integration Tests

```bash
cd tests
dotnet test
```

### Load Testing

Use tools like Apache JMeter or k6:

```bash
k6 run load-test.js
```

## 📦 Production Deployment

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

Example with GitHub Actions:

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

## 📝 Project Structure

```
ecommerce-platform/
├── src/
│   ├── Shared/                 # Shared code
│   │   ├── Events/            # Integration events
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

## 🤝 Contributing

1. Fork the project
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License.

## 🙏 Acknowledgments

- .NET Core Team
- Docker Community
- RabbitMQ
- Consul by HashiCorp
- Elastic Stack

## 📞 Support

For questions and support:
- Open an issue on GitHub
- Email: renato19mm@gmail.com
