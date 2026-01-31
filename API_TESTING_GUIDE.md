# Guia de Testes da API

Este guia contém exemplos de requisições para testar todos os endpoints da plataforma e-commerce.

## Setup Inicial

Todas as requisições devem ser feitas através do API Gateway:
```
Base URL: http://localhost:8080
```

## 1. User Service

### 1.1 Registrar Novo Usuário

**Endpoint:** `POST /api/users/register`

**Request Body:**
```json
{
  "fullName": "Maria Silva",
  "email": "maria.silva@example.com",
  "password": "senha123456",
  "phoneNumber": "+5511987654321",
  "address": "Av. Paulista, 1000 - São Paulo, SP"
}
```

**cURL:**
```bash
curl -X POST http://localhost:8080/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Maria Silva",
    "email": "maria.silva@example.com",
    "password": "senha123456",
    "phoneNumber": "+5511987654321",
    "address": "Av. Paulista, 1000 - São Paulo, SP"
  }'
```

**Response:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "maria.silva@example.com",
  "fullName": "Maria Silva",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### 1.2 Login

**Endpoint:** `POST /api/users/login`

**Request Body:**
```json
{
  "email": "maria.silva@example.com",
  "password": "senha123456"
}
```

**cURL:**
```bash
curl -X POST http://localhost:8080/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "maria.silva@example.com",
    "password": "senha123456"
  }'
```

**Response:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "maria.silva@example.com",
  "fullName": "Maria Silva",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### 1.3 Obter Dados do Usuário

**Endpoint:** `GET /api/users/{userId}`

**cURL:**
```bash
curl http://localhost:8080/api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

## 2. Product Service

### 2.1 Listar Todos os Produtos

**Endpoint:** `GET /api/products`

**Query Parameters:**
- `searchTerm` (opcional): Termo de busca
- `category` (opcional): Categoria do produto
- `minPrice` (opcional): Preço mínimo
- `maxPrice` (opcional): Preço máximo
- `page` (opcional, default: 1): Número da página
- `pageSize` (opcional, default: 20): Itens por página

**cURL:**
```bash
# Listar todos
curl http://localhost:8080/api/products

# Com filtros
curl "http://localhost:8080/api/products?category=Electronics&minPrice=500&maxPrice=2000&page=1&pageSize=10"

# Buscar por termo
curl "http://localhost:8080/api/products?searchTerm=laptop"
```

**Response:**
```json
{
  "totalCount": 3,
  "page": 1,
  "pageSize": 20,
  "products": [
    {
      "id": "8e3d6f42-1234-5678-9abc-def012345678",
      "name": "Laptop Dell XPS 15",
      "description": "High-performance laptop with Intel i7, 16GB RAM, 512GB SSD",
      "price": 1499.99,
      "stockQuantity": 50,
      "category": "Electronics",
      "imageUrl": "https://example.com/laptop.jpg",
      "createdAt": "2025-01-31T10:00:00Z",
      "isActive": true
    }
  ]
}
```

### 2.2 Obter Produto Específico

**Endpoint:** `GET /api/products/{productId}`

**cURL:**
```bash
curl http://localhost:8080/api/products/8e3d6f42-1234-5678-9abc-def012345678
```

### 2.3 Criar Novo Produto

**Endpoint:** `POST /api/products`

**Request Body:**
```json
{
  "name": "Smartwatch Apple Watch Series 9",
  "description": "Advanced smartwatch with health monitoring",
  "price": 599.99,
  "stockQuantity": 120,
  "category": "Electronics",
  "imageUrl": "https://example.com/watch.jpg"
}
```

**cURL:**
```bash
curl -X POST http://localhost:8080/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Smartwatch Apple Watch Series 9",
    "description": "Advanced smartwatch with health monitoring",
    "price": 599.99,
    "stockQuantity": 120,
    "category": "Electronics",
    "imageUrl": "https://example.com/watch.jpg"
  }'
```

### 2.4 Atualizar Produto

**Endpoint:** `PUT /api/products/{productId}`

**Request Body:**
```json
{
  "name": "Laptop Dell XPS 15 - Updated",
  "price": 1399.99,
  "stockQuantity": 45
}
```

**cURL:**
```bash
curl -X PUT http://localhost:8080/api/products/8e3d6f42-1234-5678-9abc-def012345678 \
  -H "Content-Type: application/json" \
  -d '{
    "price": 1399.99,
    "stockQuantity": 45
  }'
```

### 2.5 Listar Categorias

**Endpoint:** `GET /api/products/categories`

**cURL:**
```bash
curl http://localhost:8080/api/products/categories
```

## 3. Cart Service

### 3.1 Obter Carrinho do Usuário

**Endpoint:** `GET /api/cart/{userId}`

**cURL:**
```bash
curl http://localhost:8080/api/cart/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Response:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "items": [
    {
      "productId": "8e3d6f42-1234-5678-9abc-def012345678",
      "productName": "Laptop Dell XPS 15",
      "quantity": 2,
      "price": 1499.99
    }
  ],
  "updatedAt": "2025-01-31T14:30:00Z"
}
```

### 3.2 Adicionar Item ao Carrinho

**Endpoint:** `POST /api/cart/{userId}/items`

**Request Body:**
```json
{
  "productId": "8e3d6f42-1234-5678-9abc-def012345678",
  "productName": "Laptop Dell XPS 15",
  "quantity": 1,
  "price": 1499.99
}
```

**cURL:**
```bash
curl -X POST http://localhost:8080/api/cart/3fa85f64-5717-4562-b3fc-2c963f66afa6/items \
  -H "Content-Type: application/json" \
  -d '{
    "productId": "8e3d6f42-1234-5678-9abc-def012345678",
    "productName": "Laptop Dell XPS 15",
    "quantity": 1,
    "price": 1499.99
  }'
```

### 3.3 Atualizar Quantidade de Item

**Endpoint:** `PUT /api/cart/{userId}/items/{productId}`

**Request Body:**
```json
{
  "quantity": 3
}
```

**cURL:**
```bash
curl -X PUT http://localhost:8080/api/cart/3fa85f64-5717-4562-b3fc-2c963f66afa6/items/8e3d6f42-1234-5678-9abc-def012345678 \
  -H "Content-Type: application/json" \
  -d '{
    "quantity": 3
  }'
```

### 3.4 Remover Item do Carrinho

**Endpoint:** `DELETE /api/cart/{userId}/items/{productId}`

**cURL:**
```bash
curl -X DELETE http://localhost:8080/api/cart/3fa85f64-5717-4562-b3fc-2c963f66afa6/items/8e3d6f42-1234-5678-9abc-def012345678
```

### 3.5 Limpar Carrinho

**Endpoint:** `DELETE /api/cart/{userId}`

**cURL:**
```bash
curl -X DELETE http://localhost:8080/api/cart/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

## 4. Order Service

### 4.1 Criar Pedido

**Endpoint:** `POST /api/orders`

**Request Body:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "items": [
    {
      "productId": "8e3d6f42-1234-5678-9abc-def012345678",
      "productName": "Laptop Dell XPS 15",
      "quantity": 1,
      "price": 1499.99
    }
  ],
  "totalAmount": 1499.99,
  "shippingAddress": "Av. Paulista, 1000 - São Paulo, SP"
}
```

**cURL:**
```bash
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "items": [
      {
        "productId": "8e3d6f42-1234-5678-9abc-def012345678",
        "productName": "Laptop Dell XPS 15",
        "quantity": 1,
        "price": 1499.99
      }
    ],
    "totalAmount": 1499.99,
    "shippingAddress": "Av. Paulista, 1000 - São Paulo, SP"
  }'
```

**Response:**
```json
{
  "id": "9b2e7f53-2345-6789-0abc-def123456789",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "totalAmount": 1499.99,
  "status": "Pending",
  "shippingAddress": "Av. Paulista, 1000 - São Paulo, SP",
  "createdAt": "2025-01-31T15:00:00Z",
  "items": [
    {
      "id": "item-id",
      "orderId": "9b2e7f53-2345-6789-0abc-def123456789",
      "productId": "8e3d6f42-1234-5678-9abc-def012345678",
      "productName": "Laptop Dell XPS 15",
      "quantity": 1,
      "price": 1499.99
    }
  ]
}
```

### 4.2 Obter Pedido Específico

**Endpoint:** `GET /api/orders/{orderId}`

**cURL:**
```bash
curl http://localhost:8080/api/orders/9b2e7f53-2345-6789-0abc-def123456789
```

### 4.3 Obter Pedidos do Usuário

**Endpoint:** `GET /api/orders/user/{userId}`

**cURL:**
```bash
curl http://localhost:8080/api/orders/user/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

### 4.4 Atualizar Status do Pedido

**Endpoint:** `PUT /api/orders/{orderId}/status`

**Request Body:**
```json
"Shipped"
```

Valores possíveis:
- `Pending`
- `PaymentProcessing`
- `PaymentFailed`
- `PaymentConfirmed`
- `Shipped`
- `Delivered`
- `Cancelled`

**cURL:**
```bash
curl -X PUT http://localhost:8080/api/orders/9b2e7f53-2345-6789-0abc-def123456789/status \
  -H "Content-Type: application/json" \
  -d '"Shipped"'
```

## 5. Payment Service

### 5.1 Processar Pagamento

**Endpoint:** `POST /api/payments/process`

**Request Body:**
```json
{
  "orderId": "9b2e7f53-2345-6789-0abc-def123456789",
  "amount": 1499.99,
  "currency": "usd",
  "paymentMethodId": "pm_card_visa",
  "customerEmail": "maria.silva@example.com"
}
```

**cURL:**
```bash
curl -X POST http://localhost:8080/api/payments/process \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "9b2e7f53-2345-6789-0abc-def123456789",
    "amount": 1499.99,
    "currency": "usd",
    "paymentMethodId": "pm_card_visa",
    "customerEmail": "maria.silva@example.com"
  }'
```

**Response:**
```json
{
  "paymentId": "payment-id",
  "success": true,
  "transactionId": "pi_stripe_transaction_id",
  "errorMessage": null
}
```

## Fluxo Completo de Compra

Aqui está um exemplo de fluxo completo de compra:

```bash
# 1. Registrar usuário
USER_RESPONSE=$(curl -s -X POST http://localhost:8080/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "João Silva",
    "email": "joao@example.com",
    "password": "senha123",
    "phoneNumber": "+5511999999999",
    "address": "Rua Example, 123"
  }')

USER_ID=$(echo $USER_RESPONSE | jq -r '.userId')
TOKEN=$(echo $USER_RESPONSE | jq -r '.token')

# 2. Listar produtos
PRODUCTS=$(curl -s http://localhost:8080/api/products)
PRODUCT_ID=$(echo $PRODUCTS | jq -r '.products[0].id')
PRODUCT_NAME=$(echo $PRODUCTS | jq -r '.products[0].name')
PRODUCT_PRICE=$(echo $PRODUCTS | jq -r '.products[0].price')

# 3. Adicionar ao carrinho
curl -X POST http://localhost:8080/api/cart/$USER_ID/items \
  -H "Content-Type: application/json" \
  -d "{
    \"productId\": \"$PRODUCT_ID\",
    \"productName\": \"$PRODUCT_NAME\",
    \"quantity\": 1,
    \"price\": $PRODUCT_PRICE
  }"

# 4. Criar pedido
ORDER_RESPONSE=$(curl -s -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{
    \"userId\": \"$USER_ID\",
    \"items\": [{
      \"productId\": \"$PRODUCT_ID\",
      \"productName\": \"$PRODUCT_NAME\",
      \"quantity\": 1,
      \"price\": $PRODUCT_PRICE
    }],
    \"totalAmount\": $PRODUCT_PRICE,
    \"shippingAddress\": \"Rua Example, 123\"
  }")

ORDER_ID=$(echo $ORDER_RESPONSE | jq -r '.id')

# 5. Processar pagamento
curl -X POST http://localhost:8080/api/payments/process \
  -H "Content-Type: application/json" \
  -d "{
    \"orderId\": \"$ORDER_ID\",
    \"amount\": $PRODUCT_PRICE,
    \"currency\": \"usd\",
    \"paymentMethodId\": \"pm_card_visa\",
    \"customerEmail\": \"joao@example.com\"
  }"

# 6. Verificar status do pedido
curl http://localhost:8080/api/orders/$ORDER_ID
```

## Postman Collection

Você pode importar esta collection no Postman para facilitar os testes:

```json
{
  "info": {
    "name": "E-Commerce Platform API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "User Service",
      "item": [
        {
          "name": "Register",
          "request": {
            "method": "POST",
            "url": "{{base_url}}/api/users/register",
            "body": {
              "mode": "raw",
              "raw": "{\n  \"fullName\": \"Test User\",\n  \"email\": \"test@example.com\",\n  \"password\": \"password123\"\n}"
            }
          }
        }
      ]
    }
  ],
  "variable": [
    {
      "key": "base_url",
      "value": "http://localhost:8080"
    }
  ]
}
```

## Dicas de Teste

1. **Autenticação**: Guarde o token JWT retornado no login e use-o no header `Authorization: Bearer {token}`

2. **IDs**: Os IDs são GUIDs. Salve os IDs retornados nas respostas para usar em requisições subsequentes

3. **Ordem de Teste**: Siga a ordem: User → Product → Cart → Order → Payment

4. **Verificar Logs**: Use `docker-compose logs -f {service-name}` para ver os logs em tempo real

5. **RabbitMQ**: Acesse http://localhost:15672 para ver as mensagens sendo processadas

6. **Kibana**: Acesse http://localhost:5601 para visualizar logs centralizados
