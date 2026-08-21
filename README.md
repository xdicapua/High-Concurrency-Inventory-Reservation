# ⚡ High-Concurrency Flash Sale & Inventory Reservation Engine

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-13-239120?logo=c-sharp)
![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)
![k6](https://img.shields.io/badge/k6-Load%20Testing-7D64FF?logo=k6)

Motor distribuido de reservas de inventario de ultra baja latencia diseñado para escenarios de alta concurrencia (*flash sales*, *ticketing*, *drops* de productos).

Implementa el patrón **Two-Phase Stock Allocation**, separando la contención de concurrencia en memoria RAM (Redis + Lua) de la persistencia transaccional duradera en disco (PostgreSQL + EF Core), garantizando **Zero Overselling** y aislamiento estricto bajo condiciones de carrera extremas.

---

## 🎯 Arquitectura del Sistema

```text
                       [ Clientes Concurrentes ]
                                   │
                                   │ (HTTP POST /reservations)
                                   ▼
                        ┌─────────────────────┐
                        │   ASP.NET Core 10   │
                        │    Minimal APIs     │
                        └──────────┬──────────┘
                                   │
              ┌────────────────────┴────────────────────┐
              ▼                                         ▼
    [ Fase 1: Concurrencia ]                  [ Fase 2: Persistencia ]
    ┌──────────────────────┐                  ┌──────────────────────┐
    │   Redis Engine RAM   │                  │  PostgreSQL Storage  │
    │   (StackExchange)    │                  │  (EF Core Relational)│
    └──────────┬───────────┘                  └──────────┬───────────┘
               │                                         │
 ┌─────────────┴─────────────┐             ┌─────────────┴─────────────┐
 │ 1. Atomic Lua Script      │             │ 1. Check Redis State      │
 │ 2. EVAL GET & DECR        │             │ 2. Confirm Domain Entity  │
 │ 3. Temporary Hash + TTL   │             │ 3. Write Reservations Row │
 │ 4. Fast Reject (409)      │             │ 4. Evict Redis Cache      │
 └───────────────────────────┘             └───────────────────────────┘
```

---

## 🏛️ Clean Architecture

El proyecto sigue una separación estricta de responsabilidades en 4 capas:

* **`FlashSale.Domain`**: Entidades y reglas de negocio puras sin dependencias externas ([`Product`](src/FlashSale.Domain/Entities/Product.cs), [`Reservation`](src/FlashSale.Domain/Entities/Reservation.cs), [`ReservationStatus`](src/FlashSale.Domain/Entities/ReservationStatus.cs)).
* **`FlashSale.Application`**: Casos de uso ([`ReserveStockUseCase`](src/FlashSale.Application/UseCases/ReserveStockUseCase.cs), [`ConfirmReservationUseCase`](src/FlashSale.Application/UseCases/ConfirmReservationUseCase.cs)), interfaces de repositorio y DTOs.
* **`FlashSale.Infrastructure`**: Implementaciones técnicas con Redis vía scripts Lua atómicos ([`RedisInventoryCacheRepository`](src/FlashSale.Infrastructure/Cache/RedisInventoryCacheRepository.cs)) y PostgreSQL con EF Core ([`ApplicationDbContext`](src/FlashSale.Infrastructure/Persistence/ApplicationDbContext.cs), [`ReservationRepository`](src/FlashSale.Infrastructure/Repositories/ReservationRepository.cs)).
* **`FlashSale.Api`**: Endpoints Minimal API, configuración del pipeline HTTP e inyección de dependencias.

---

## 🔌 Referencia de la API (Endpoints)

### 1. Reservar Stock en RAM (Ultra Baja Latencia)
* **Endpoint:** `POST /api/v1/reservations`
* **Descripción:** Ejecuta el script Lua en Redis para validar stock, decrementar atómicamente y crear reserva temporal con TTL (10 min).
* **Payload:**
```json
{
  "sku": "PROD-001",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```
* **Respuestas:**
  * `200 OK`: Reserva creada exitosamente (retorna `reservationId` y `expiresAtUtc`).
  * `409 Conflict`: Stock agotado (`"Stock agotado para este producto."`).

### 2. Confirmar y Persistir Reserva en PostgreSQL
* **Endpoint:** `POST /api/v1/reservations/confirm`
* **Descripción:** Valida la vigencia de la reserva en Redis, instancia y confirma la entidad de dominio, persiste el registro en PostgreSQL y remueve la reserva de la memoria RAM.
* **Payload:**
```json
{
  "reservationId": "a9c687e1-8f4b-4b13-a442-99529ff709b1",
  "productId": "7b2e61e0-3f4a-4a21-8efb-112233445566"
}
```
* **Respuestas:**
  * `200 OK`: Confirmada y almacenada en base de datos.
  * `400 Bad Request`: Reserva inexistente o expirada.

---

## 🚀 Benchmark & Resultados de Rendimiento (k6)

Prueba de estrés continuo simulando **50 usuarios concurrentes** golpeando la API en paralelo durante **10 segundos**:

| Métrica | Resultado Obtenido | Relevancia Técnica |
|---|---|---|
| **Throughput (RPS)** | **~11,494 req/s** | Capacidad masiva en nodo único |
| **Total Solicitudes** | **114,958 requests** | Carga total procesada sin saturación |
| **Integridad de Stock** | **100 / 100 Reservas** | Zero Overselling comprobado |
| **Rechazos Limpios (409)**| **114,858 requests** | Filtrado atómico en RAM sin tocar disco |
| **Latencia p(95)** | **5.11 ms** | 95% de solicitudes en menos de 6 ms |
| **Latencia p(99)** | **9.37 ms** | Percentil 99 resuelto en menos de 10 ms |
| **Tasa de Errores No Controlados** | **0.00%** | Resiliencia total de hilos asíncronos |

---

## 🛠️ Stack Tecnológico

* **Lenguaje & Framework:** C# / .NET 10 (ASP.NET Core Minimal APIs)
* **Concurrencia en Memoria:** Redis 7 + StackExchange.Redis + Scripts Lua
* **Base de Datos Relacional:** PostgreSQL 16 + Entity Framework Core (Npgsql)
* **Contenedores:** Docker & Docker Compose
* **Pruebas de Carga:** Grafana k6

---

## ⚙️ Guía de Ejecución Local

```bash
# 1. Levantar contenedores (Redis & PostgreSQL)
docker-compose up -d

# 2. Aplicar migraciones en PostgreSQL
dotnet ef database update --project src/FlashSale.Infrastructure/FlashSale.Infrastructure.csproj --startup-project src/FlashSale.Api/FlashSale.Api.csproj

# 3. Cargar stock inicial en Redis
docker exec -it flash-sale-redis redis-cli SET item:PROD-001:stock 100

# 4. Iniciar API en modo Release
dotnet run --project src/FlashSale.Api/FlashSale.Api.csproj -c Release

# 5. Ejecutar prueba de carga con k6
k6 run load-tests/reservation-test.js
```

<br>

---

# 🇬🇧 English Version

# ⚡ High-Concurrency Flash Sale & Inventory Reservation Engine

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-13-239120?logo=c-sharp)
![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)
![k6](https://img.shields.io/badge/k6-Load%20Testing-7D64FF?logo=k6)

Ultra-low latency distributed inventory reservation engine designed for high-concurrency scenarios (flash sales, ticketing, product drops).

Implements the **Two-Phase Stock Allocation** pattern, decoupling concurrency contention in RAM (Redis + Lua) from durable transactional persistence on disk (PostgreSQL + EF Core). This guarantees **Zero Overselling** and strict data isolation under extreme race conditions.

---

## 🎯 System Architecture

```text
                       [ Concurrent Clients ]
                                   │
                                   │ (HTTP POST /reservations)
                                   ▼
                        ┌─────────────────────┐
                        │   ASP.NET Core 10   │
                        │    Minimal APIs     │
                        └──────────┬──────────┘
                                   │
              ┌────────────────────┴────────────────────┐
              ▼                                         ▼
    [ Phase 1: Concurrency ]                  [ Phase 2: Persistence ]
    ┌──────────────────────┐                  ┌──────────────────────┐
    │   Redis Engine RAM   │                  │  PostgreSQL Storage  │
    │   (StackExchange)    │                  │  (EF Core Relational)│
    └──────────┬───────────┘                  └──────────┬───────────┘
               │                                         │
 ┌─────────────┴─────────────┐             ┌─────────────┴─────────────┐
 │ 1. Atomic Lua Script      │             │ 1. Check Redis State      │
 │ 2. EVAL GET & DECR        │             │ 2. Confirm Domain Entity  │
 │ 3. Temporary Hash + TTL   │             │ 3. Write Reservations Row │
 │ 4. Fast Reject (409)      │             │ 4. Evict Redis Cache      │
 └───────────────────────────┘             └───────────────────────────┘
```

---

## 🏛️ Clean Architecture

The project follows strict separation of concerns across 4 layers:

* **`FlashSale.Domain`**: Pure domain entities and business rules without external dependencies ([`Product`](src/FlashSale.Domain/Entities/Product.cs), [`Reservation`](src/FlashSale.Domain/Entities/Reservation.cs), [`ReservationStatus`](src/FlashSale.Domain/Entities/ReservationStatus.cs)).
* **`FlashSale.Application`**: Use cases ([`ReserveStockUseCase`](src/FlashSale.Application/UseCases/ReserveStockUseCase.cs), [`ConfirmReservationUseCase`](src/FlashSale.Application/UseCases/ConfirmReservationUseCase.cs)), repository interfaces, and DTOs.
* **`FlashSale.Infrastructure`**: Technical implementations using Redis with atomic Lua scripts ([`RedisInventoryCacheRepository`](src/FlashSale.Infrastructure/Cache/RedisInventoryCacheRepository.cs)) and PostgreSQL with EF Core ([`ApplicationDbContext`](src/FlashSale.Infrastructure/Persistence/ApplicationDbContext.cs), [`ReservationRepository`](src/FlashSale.Infrastructure/Repositories/ReservationRepository.cs)).
* **`FlashSale.Api`**: Minimal API endpoints, HTTP pipeline configuration, and dependency injection.

---

## 🔌 API Reference

### 1. Reserve Stock in RAM (Ultra-Low Latency)
* **Endpoint:** `POST /api/v1/reservations`
* **Description:** Executes atomic Lua script in Redis to validate stock, atomically decrement, and create temporary reservation with TTL (10 min).
* **Payload:**
```json
{
  "sku": "PROD-001",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```
* **Responses:**
  * `200 OK`: Successfully reserved (returns `reservationId` and `expiresAtUtc`).
  * `409 Conflict`: Out of stock (`"Stock agotado para este producto."`).

### 2. Confirm & Persist Reservation in PostgreSQL
* **Endpoint:** `POST /api/v1/reservations/confirm`
* **Description:** Verifies active status in Redis, instantiates and confirms domain entity, writes durable record to PostgreSQL, and evicts RAM cache.
* **Payload:**
```json
{
  "reservationId": "a9c687e1-8f4b-4b13-a442-99529ff709b1",
  "productId": "7b2e61e0-3f4a-4a21-8efb-112233445566"
}
```
* **Responses:**
  * `200 OK`: Confirmed and persisted to database.
  * `400 Bad Request`: Reservation does not exist or has expired.

---

## 🚀 Benchmark & Performance Results (k6)

Continuous stress test simulating **50 concurrent virtual users** hitting the API in parallel for **10 seconds**:

| Metric | Result | Technical Significance |
|---|---|---|
| **Throughput (RPS)** | **~11,494 req/s** | Massive single-node throughput |
| **Total Requests** | **114,958 requests** | Total load processed without bottleneck |
| **Stock Integrity** | **100 / 100 Reservations** | Verified Zero Overselling |
| **Clean Rejections (409)**| **114,858 requests** | Atomic in-memory filtering without disk access |
| **Latency p(95)** | **5.11 ms** | 95% of requests completed under 6 ms |
| **Latency p(99)** | **9.37 ms** | 99th percentile resolved under 10 ms |
| **Unhandled Error Rate** | **0.00%** | Full asynchronous thread resilience |

---

## 🛠️ Tech Stack

* **Language & Framework:** C# / .NET 10 (ASP.NET Core Minimal APIs)
* **In-Memory Concurrency:** Redis 7 + StackExchange.Redis + Lua Scripts
* **Relational Database:** PostgreSQL 16 + Entity Framework Core (Npgsql)
* **Containers:** Docker & Docker Compose
* **Load Testing:** Grafana k6

---

## ⚙️ Local Quickstart Guide

```bash
# 1. Start containers (Redis & PostgreSQL)
docker-compose up -d

# 2. Apply PostgreSQL database migrations
dotnet ef database update --project src/FlashSale.Infrastructure/FlashSale.Infrastructure.csproj --startup-project src/FlashSale.Api/FlashSale.Api.csproj

# 3. Seed initial inventory in Redis
docker exec -it flash-sale-redis redis-cli SET item:PROD-001:stock 100

# 4. Run API in Release mode
dotnet run --project src/FlashSale.Api/FlashSale.Api.csproj -c Release

# 5. Execute k6 load test
k6 run load-tests/reservation-test.js
```