# ⚡ High-Concurrency Flash Sale & Inventory Reservation Engine

Motor distribuido de reservas de inventario de ultra baja latencia diseñado para escenarios de alta concurrencia (flash sales, ticketing, drops de productos).

Implementa el patron Two-Phase Stock Allocation, separando la contencion de concurrencia en memoria RAM (Redis + Lua) de la persistencia transaccional duradera en disco (PostgreSQL + EF Core), garantizando Zero Overselling y aislamiento estricto bajo condiciones de carrera.

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

El proyecto sigue la separacion de responsabilidades en 4 capas:

* **`FlashSale.Domain`**: Entidades y reglas del negocio puras (`Product`, `Reservation`).
* **`FlashSale.Application`**: Casos de uso (`ReserveStockUseCase`, `ConfirmReservationUseCase`), interfaces y DTOs.
* **`FlashSale.Infrastructure`**: Implementaciones tecnicas con Redis (scripts Lua) y PostgreSQL (`ApplicationDbContext`).
* **`FlashSale.Api`**: Endpoints Minimal API e inyeccion de dependencias.

---

## 🚀 Benchmark & Resultados de Rendimiento (k6)

Prueba de estres continuo simulando 50 usuarios concurrentes golpeando la API en paralelo durante 10 segundos:

| Metrica | Resultado Obtenido | Relevancia Tecnica |
|---|---|---|
| **Throughput (RPS)** | **~11,494 req/s** | Capacidad masiva en nodo unico |
| **Total Solicitudes** | **114,958 requests** | Carga total procesada sin saturacion |
| **Integridad de Stock** | **100 / 100 Reservas** | Zero Overselling comprobado |
| **Rechazos Limpios (409)**| **114,858 requests** | Filtrado atomico en RAM sin tocar disco |
| **Latencia p(95)** | **5.11 ms** | 95% de solicitudes en menos de 6 ms |
| **Latencia p(99)** | **9.37 ms** | Percentil 99 resuelto en menos de 10 ms |
| **Tasa de Errores No Controlados** | **0.00%** | Resiliencia de hilos asincronos |

---

## 🛠️ Stack Tecnologico

* **Lenguaje & Framework:** C# / .NET 10 (ASP.NET Core Minimal APIs)
* **Concurrencia en Memoria:** Redis 7 + StackExchange.Redis + Scripts Lua
* **Base de Datos Relacional:** PostgreSQL 16 + Entity Framework Core
* **Contenedores:** Docker & Docker Compose
* **Pruebas de Carga:** Grafana k6

---

## ⚙️ Guia de Ejecucion Local

```bash
# 1. Levantar contenedores
docker-compose up -d

# 2. Aplicar migraciones en PostgreSQL
dotnet ef database update --project src/FlashSale.Infrastructure/FlashSale.Infrastructure.csproj --startup-project src/FlashSale.Api/FlashSale.Api.csproj

# 3. Cargar stock en Redis
docker exec -it flash-sale-redis redis-cli SET item:PROD-001:stock 100

# 4. Iniciar API
dotnet run --project src/FlashSale.Api/FlashSale.Api.csproj -c Release

# 5. Ejecutar prueba de carga
k6 run load-tests/reservation-test.js
```