# 🛡️ TradeGuard — Protecting Your Trades in Real-Time  

**TradeGuard** is a **real-time risk engine** for trading platforms.  
Think of it as a **guardian angel** 🕊️ sitting between your **trade orders** and the **market**, making sure:  

✔️ You’re not over-exposed  
✔️ You have enough margin  
✔️ You’re not spamming trades like a HFT bot gone rogue 🚀  

---

## ✨ Why TradeGuard?
Modern trading platforms process thousands of orders per second. One bad trade could:  
- Blow past exposure limits 💥  
- Eat up all your margin 🏦  
- Flood the system with reckless orders 🌀  

That’s where **TradeGuard** comes in:  
- Every **order** goes through a **risk checkpoint**  
- Decisions (Approve ✅ / Reject ❌) are stored & audited  
- Results are **published back** to your event bus in milliseconds  

---

## 🏗️ How It Works  

```
┌─────────────┐       ┌───────────────┐
│ OrderSubmit │─────▶│ RabbitMQ Bus   │─────┐
└─────────────┘       └───────────────┘      │
                                             ▼
                                    ┌────────────────────┐
                                    │ TradeGuard Worker  │
                                    │ (BackgroundService)│
                                    └────────┬───────────┘
                                             │
                         ┌───────────────────┼───────────────────┐
                         ▼                   ▼                   ▼
                  Exposure Check      Margin Check       Velocity Check
                         │                   │                   │
                         └──────────┬────────┴──────────┬────────┘
                                    ▼                   ▼
                        ┌───────────────────┐   ┌───────────────────┐
                        │ EF Core + PGSQL   │   │ OrderDecision Msg │
                        │ (Persist orders   │   │ (Published back   │
                        │  + audit logs)    │   │   to RabbitMQ)    │
                        └───────────────────┘   └───────────────────┘

```

📩 **Step 1: Order Submitted**  
A trader sends an order → lands in RabbitMQ (`orders.submitted`).  

🧠 **Step 2: Risk Engine Thinks**  
`TradeGuardWorker` wakes up, grabs the order, and runs **three risk gauntlets**:  
- **Exposure Check** → “Too much on the line?”  
- **Margin Check** → “Do you have enough cash for this?”  
- **Velocity Check** → “Are you trading like a caffeinated squirrel?” 🐿️  

🗂️ **Step 3: Audit Everything**  
Decisions are stored in **PostgreSQL** (orders + per-check results).  

📢 **Step 4: Verdict Announced**  
Publishes the **final decision** (`orders.decided`) → “Approved” or “Rejected (Reason: …)”.  

---

## ⚙️ Tech Stack
- **.NET 8** – performance, modern C#  
- **Entity Framework Core + PostgreSQL** – persistence  
- **RabbitMQ** – event-driven heart of the system  
- **Clean Architecture** – Core, Infrastructure, API  

---

## 🚀 Quick Start

### 1️⃣ Spin up RabbitMQ & Postgres
```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
docker run -d --name postgres -e POSTGRES_USER=trade -e POSTGRES_PASSWORD=guard -e POSTGRES_DB=tradeguard -p 5432:5432 postgres
```

### 2️⃣ Run the Worker
```bash
dotnet run --project API
Swagger UI → http://localhost:5000/swagger
```

### 3️⃣ Submit an Order
Publish to orders.submitted:
```
{
  "orderId": "9f2f3f70-2d55-41b3-9e87-4c36a5a90123",
  "accountId": "a1b2c3d4-e5f6-47a8-b9c0-1234567890ab",
  "ticker": "AAPL",
  "action": "Buy",
  "actionMode": "Market",
  "numberOfShares": 50,
  "pricePerShare": 150,
  "stopLossPrice": null,
  "submittedAtUtc": "2025-08-31T10:00:00Z"
}
```
### 4️⃣ Get the Decision
From orders.decided:
```
{
  "orderId": "9f2f3f70-2d55-41b3-9e87-4c36a5a90123",
  "approved": false,
  "reason": "Exposure limit exceeded"
}
```
---
## 🔍 Risk Rules Explained
| Risk Check   | What It Prevents                  | Example                       |
| ------------ | --------------------------------- | ----------------------------- |
| **Exposure** | Account exceeding position limits | No “YOLO” all-in trades 🎲    |
| **Margin**   | Trading without enough collateral | Stops you from going broke 💸 |
| **Velocity** | Too many trades too fast          | Stops “bot storms” 🌪️         |

---
## 🛠 Dev Goodies
* GET /health → Health check
* POST /bus/publish → Publish test messages into RabbitMQ
* Built-in logging for every decision

---
## 📂 Project Structure
```
Core/               # Entities + Interfaces + Services
Infrastructure/     # Adapters (DB, RabbitMQ)
API/                # Worker + DI setup + Minimal APIs
```

---
## 🌱 Future Roadmap
* 📊 Sector Risk — limit exposure per industry
*⚡ ML-based Fraud Detection — smarter anomaly detection
* ☁️ Kubernetes Scaling — auto-scale workers with order load
* 🔗 gRPC API — external risk queries in real-time

## 🤝 Contributing
PRs are welcome! 🚀
If you’d like to add risk models, new adapters, or improve resilience, fork the repo and submit your ideas.

##📜 License
MIT — free to use, and improve.





