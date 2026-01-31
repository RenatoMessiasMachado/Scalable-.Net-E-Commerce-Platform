.PHONY: help build up down logs clean restart test

help: ## Mostra esta ajuda
	@echo "E-Commerce Microservices Platform - Comandos Disponíveis"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}'

build: ## Constrói todas as imagens Docker
	docker-compose build

up: ## Inicia todos os serviços
	./start.sh

down: ## Para todos os serviços
	docker-compose down

logs: ## Exibe logs de todos os serviços
	docker-compose logs -f

logs-user: ## Exibe logs do User Service
	docker-compose logs -f user-service

logs-product: ## Exibe logs do Product Service
	docker-compose logs -f product-service

logs-cart: ## Exibe logs do Cart Service
	docker-compose logs -f cart-service

logs-order: ## Exibe logs do Order Service
	docker-compose logs -f order-service

logs-payment: ## Exibe logs do Payment Service
	docker-compose logs -f payment-service

logs-notification: ## Exibe logs do Notification Service
	docker-compose logs -f notification-service

clean: ## Remove todos os containers, volumes e imagens
	docker-compose down -v --rmi all
	docker system prune -af

restart: ## Reinicia todos os serviços
	docker-compose restart

restart-user: ## Reinicia User Service
	docker-compose restart user-service

restart-product: ## Reinicia Product Service
	docker-compose restart product-service

restart-cart: ## Reinicia Cart Service
	docker-compose restart cart-service

restart-order: ## Reinicia Order Service
	docker-compose restart order-service

restart-payment: ## Reinicia Payment Service
	docker-compose restart payment-service

restart-notification: ## Reinicia Notification Service
	docker-compose restart notification-service

ps: ## Lista status dos containers
	docker-compose ps

scale-product: ## Escala Product Service para 3 instâncias
	docker-compose up -d --scale product-service=3

scale-order: ## Escala Order Service para 2 instâncias
	docker-compose up -d --scale order-service=2

exec-user: ## Acessa shell do User Service
	docker-compose exec user-service /bin/bash

exec-db: ## Acessa PostgreSQL do User Service
	docker-compose exec userdb psql -U postgres -d userdb

exec-redis: ## Acessa Redis CLI
	docker-compose exec redis redis-cli

test: ## Executa testes
	dotnet test

init-db: ## Inicializa bancos de dados com dados de exemplo
	@echo "Inicializando bancos de dados..."
	docker-compose exec userdb psql -U postgres -d userdb -c "SELECT 1"
	docker-compose exec productdb psql -U postgres -d productdb -c "SELECT 1"
	docker-compose exec orderdb psql -U postgres -d orderdb -c "SELECT 1"

backup-db: ## Faz backup dos bancos de dados
	@echo "Fazendo backup dos bancos de dados..."
	docker-compose exec -T userdb pg_dump -U postgres userdb > backup_userdb_$$(date +%Y%m%d_%H%M%S).sql
	docker-compose exec -T productdb pg_dump -U postgres productdb > backup_productdb_$$(date +%Y%m%d_%H%M%S).sql
	docker-compose exec -T orderdb pg_dump -U postgres orderdb > backup_orderdb_$$(date +%Y%m%d_%H%M%S).sql

health: ## Verifica saúde de todos os serviços
	@echo "Verificando saúde dos serviços..."
	@curl -f http://localhost:5001/health && echo "✓ User Service OK" || echo "✗ User Service FAIL"
	@curl -f http://localhost:5002/health && echo "✓ Product Service OK" || echo "✗ Product Service FAIL"
	@curl -f http://localhost:5003/health && echo "✓ Cart Service OK" || echo "✗ Cart Service FAIL"
	@curl -f http://localhost:5004/health && echo "✓ Order Service OK" || echo "✗ Order Service FAIL"
	@curl -f http://localhost:5005/health && echo "✓ Payment Service OK" || echo "✗ Payment Service FAIL"
	@curl -f http://localhost:5006/health && echo "✓ Notification Service OK" || echo "✗ Notification Service FAIL"

dev: ## Inicia ambiente de desenvolvimento
	docker-compose up -d userdb productdb orderdb redis rabbitmq consul elasticsearch kibana

prod: ## Inicia todos os serviços para produção
	docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

stats: ## Mostra estatísticas de uso de recursos
	docker stats

network: ## Inspeciona a rede Docker
	docker network inspect ecommerce-platform_ecommerce-network

volumes: ## Lista volumes Docker
	docker volume ls | grep ecommerce

prune: ## Remove recursos não utilizados
	docker system prune -f

install: ## Instala dependências .NET
	dotnet restore

format: ## Formata código
	dotnet format

lint: ## Executa linter
	dotnet format --verify-no-changes

migration-user: ## Cria migration para User Service
	cd src/UserService && dotnet ef migrations add $(name)

migration-product: ## Cria migration para Product Service
	cd src/ProductService && dotnet ef migrations add $(name)

migration-order: ## Cria migration para Order Service
	cd src/OrderService && dotnet ef migrations add $(name)

update-db-user: ## Atualiza database do User Service
	cd src/UserService && dotnet ef database update

update-db-product: ## Atualiza database do Product Service
	cd src/ProductService && dotnet ef database update

update-db-order: ## Atualiza database do Order Service
	cd src/OrderService && dotnet ef database update
