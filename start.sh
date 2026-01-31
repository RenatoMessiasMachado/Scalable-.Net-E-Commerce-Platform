#!/bin/bash

echo "======================================"
echo "E-Commerce Microservices Platform"
echo "======================================"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}Error: Docker is not running. Please start Docker first.${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Docker is running${NC}"

# Check if Docker Compose is available
if ! command -v docker-compose &> /dev/null; then
    echo -e "${RED}Error: Docker Compose is not installed.${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Docker Compose is available${NC}"
echo ""

# Function to wait for service
wait_for_service() {
    local service=$1
    local port=$2
    local max_attempts=30
    local attempt=1

    echo -n "Waiting for $service to be ready..."
    
    while [ $attempt -le $max_attempts ]; do
        if curl -s http://localhost:$port/health > /dev/null 2>&1; then
            echo -e " ${GREEN}✓${NC}"
            return 0
        fi
        echo -n "."
        sleep 2
        ((attempt++))
    done
    
    echo -e " ${RED}✗${NC}"
    return 1
}

# Stop existing containers
echo -e "${YELLOW}Stopping existing containers...${NC}"
docker-compose down

# Build images
echo -e "${YELLOW}Building Docker images...${NC}"
docker-compose build

# Start infrastructure services first
echo -e "${YELLOW}Starting infrastructure services...${NC}"
docker-compose up -d userdb productdb orderdb redis rabbitmq consul elasticsearch kibana

# Wait for infrastructure
echo "Waiting for infrastructure services to initialize (30 seconds)..."
sleep 30

# Start microservices
echo -e "${YELLOW}Starting microservices...${NC}"
docker-compose up -d user-service product-service cart-service order-service payment-service notification-service

# Wait for services
echo "Waiting for microservices to initialize (20 seconds)..."
sleep 20

# Start API Gateway
echo -e "${YELLOW}Starting API Gateway...${NC}"
docker-compose up -d api-gateway

echo ""
echo "======================================"
echo -e "${GREEN}Platform started successfully!${NC}"
echo "======================================"
echo ""
echo "Service Endpoints:"
echo "===================="
echo -e "API Gateway:           ${GREEN}http://localhost:8080${NC}"
echo -e "User Service:          ${GREEN}http://localhost:5001/swagger${NC}"
echo -e "Product Service:       ${GREEN}http://localhost:5002/swagger${NC}"
echo -e "Cart Service:          ${GREEN}http://localhost:5003/swagger${NC}"
echo -e "Order Service:         ${GREEN}http://localhost:5004/swagger${NC}"
echo -e "Payment Service:       ${GREEN}http://localhost:5005/swagger${NC}"
echo -e "Notification Service:  ${GREEN}http://localhost:5006/swagger${NC}"
echo ""
echo "Management UIs:"
echo "===================="
echo -e "Consul:                ${GREEN}http://localhost:8500${NC}"
echo -e "RabbitMQ:              ${GREEN}http://localhost:15672${NC} (admin/admin123)"
echo -e "Kibana:                ${GREEN}http://localhost:5601${NC}"
echo ""
echo "To view logs:"
echo "  docker-compose logs -f [service-name]"
echo ""
echo "To stop all services:"
echo "  docker-compose down"
echo ""
