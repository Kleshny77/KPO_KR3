# Архитектура сервиса оплаты заказов

## Общий обзор
Система состоит из двух микросервисов и собственного API Gateway. Коммуникация между сервисами — через Kafka (асинхронно, Confluent.Kafka). Внешние клиенты взаимодействуют с системой через REST-интерфейс API Gateway. Для push-уведомлений используется SignalR. (+ я попытался привязать фронтенд, но вышло плохо...)

---

## Компоненты
| Компонент        | Описание |
|------------------|----------|
| **API Gateway**  | REST-интерфейс, маршрутизирует HTTP-запросы на сервисы, отдаёт Swagger-документацию |
| **Orders Service** | Создание заказов, получение их статусов, Outbox, SignalR push |
| **Payments Service** | Управление счетами пользователей, списание и пополнение средств, Inbox/Outbox |
| **Kafka**     | Асинхронный обмен сообщениями между сервисами (топики: order-payment-requests, order-payment-results) |
| **PostgreSQL**   | База данных для каждого сервиса (отдельная) |
| **Swagger UI**   | Документация REST API (через API Gateway) |
| **React Frontend** | Веб-интерфейс для пользователей, push через SignalR |

---

## Технологии
- Язык: **C# (.NET 8)**
- Confluent.Kafka (Kafka)
- PostgreSQL
- SignalR (push-уведомления)
- React (Vite)
- Docker, Docker Compose
- Swagger (Swashbuckle)
- Postman (коллекция)
- Unit/Integration тесты (xUnit)

---

## Доступный функционал
### Payments Service (через API Gateway)
1. **Создание счёта** (один счёт на пользователя)
2. **Пополнение счёта** (зачисление средств)
3. **Просмотр баланса** (текущий баланс пользователя)

### Orders Service (через API Gateway)
1. **Создание заказа** (асинхронно, публикует событие оплаты)
2. **Просмотр списка заказов** (все заказы пользователя)
3. **Просмотр статуса заказа** (полная информация по заказу)

---

## Поток создания заказа
1. Клиент отправляет POST `/orders/create` в API Gateway
2. Order Service создаёт заказ (статус NEW), пишет событие в Outbox
3. OutboxProcessor отправляет событие в Kafka (топик `order-payment-requests`)
4. Payments Service читает событие из Kafka, проверяет баланс, списывает деньги, пишет результат в Outbox
5. OutboxProcessor Payments Service отправляет событие (успех/неуспех оплаты) в Kafka (топик `order-payment-results`)
6. Order Service читает событие из Kafka, обновляет статус заказа (FINISHED/CANCELLED), пушит через SignalR
7. Клиент получает push-уведомление о статусе заказа

---

## Структура БД
### Orders Service
```sql
orders (
  id UUID PRIMARY KEY,
  user_id UUID,
  amount BIGINT,
  description TEXT,
  status TEXT CHECK (status IN ('NEW', 'FINISHED', 'CANCELLED')),
  created_at TIMESTAMP
)
outbox_messages (
  id UUID PRIMARY KEY,
  type TEXT,
  payload TEXT,
  sent BOOLEAN,
  created_at TIMESTAMP
)
inbox_messages (
  id INTEGER PRIMARY KEY,
  message_id TEXT,
  received_at TIMESTAMP
)
```

### Payments Service
```sql
accounts (
  id UUID PRIMARY KEY,
  user_id UUID UNIQUE,
  balance BIGINT,
  updated_at TIMESTAMP
)
transactions (
  id UUID PRIMARY KEY,
  account_id UUID,
  amount BIGINT,
  type TEXT CHECK (type IN ('DEBIT', 'CREDIT')),
  reference_order_id UUID,
  created_at TIMESTAMP
)
outbox_messages (
  id UUID PRIMARY KEY,
  type TEXT,
  payload TEXT,
  sent BOOLEAN,
  created_at TIMESTAMP
)
inbox_messages (
  id INTEGER PRIMARY KEY,
  message_id TEXT,
  received_at TIMESTAMP
)
```

---

## Kafka события и топики
### Топики:
- `order-payment-requests` — события о создании заказа (OrderPaymentRequested)
- `order-payment-results` — события о результате оплаты (PaymentProcessed)

### Пример OrderPaymentRequested
```json
{
  "orderId": "uuid",
  "userId": "uuid",
  "amount": 1000,
  "description": "тестовая покупка"
}
```
### Пример PaymentProcessed
```json
{
  "orderId": "uuid",
  "userId": "uuid",
  "amount": 1000,
  "success": true,
  "errorMessage": null
}
```

---

## Структура каталогов
```
src/
├── api-gateway/          # API Gateway (YARP)
├── order-service/        # Сервис заказов
├── payments-service/     # Сервис платежей
├── frontend/            # React приложение
└── shared/
    └── contracts/       # Общие контракты
```

---

## Inbox / Outbox
- **Outbox:** события сохраняются в БД как часть транзакции, затем отправляются в Kafka отдельным воркером (OutboxProcessor)
- **Inbox:** входящие события сохраняются перед обработкой, повторная обработка блокируется по event_id (Exactly Once)

---

## Swagger / Postman
- Swagger UI: [http://localhost:8080/swagger](http://localhost:8080/swagger)
- Postman-коллекция: в репозитории (файл postman_collection.json)

---

## Особенности
- Вся финансовая логика строго идемпотентна (CAS, транзакции)
- Асинхронность через Kafka (Confluent.Kafka)
- SignalR push-уведомления о статусе заказа
- Все сервисы и фронт в Docker, разворачиваются одной командой
- Частичное покрытие тестами 
- Поддержка Outbox/Inbox паттернов

---

## Запуск
```sh
git clone ...
cd <project>
docker-compose up --build -d
```
- Фронтенд: [http://localhost:8081](http://localhost:8081)
- Swagger: [http://localhost:8080/swagger](http://localhost:8080/swagger)

---

## Тесты
```sh
docker-compose exec order-service dotnet test
# или
cd src/orders-service && dotnet test
```
