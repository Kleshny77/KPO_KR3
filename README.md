# Микросервисная система интернет-магазина

## Сервисы
- **api-gateway** — маршрутизация запросов
- **order-service** — управление заказами, push-уведомления
- **payments-service** — управление счетами и оплатой
- **frontend** — минимальный SPA на React
- **RabbitMQ** — очередь сообщений
- **PostgreSQL** — отдельные БД для заказов и платежей

## Запуск

1. Убедитесь, что установлен Docker и Docker Compose.
2. В корне проекта выполните:

```sh
docker-compose up --build
```

3. Сервисы будут доступны по адресам:
   - Frontend: http://localhost:3000
   - API Gateway: http://localhost:8080
   - RabbitMQ Management: http://localhost:15672 (user/password)
   - Order Service: http://localhost:5001 (Swagger)
   - Payments Service: http://localhost:5002 (Swagger)

## Описание архитектуры

- Все сервисы взаимодействуют через API Gateway.
- Асинхронные процессы реализованы через RabbitMQ (Transactional Outbox/Inbox).
- Для push-уведомлений используется SignalR.
- Все сервисы и инфраструктура запускаются одной командой.

## Тесты

Для запуска тестов используйте стандартные команды dotnet test в соответствующих сервисах. 