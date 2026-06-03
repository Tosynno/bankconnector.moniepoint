

# 📌 BankConnector.Moniepoint (.NET 8)

A production-grade reference implementation of a secure **bank integration connector service** built for fintech systems integrating with MEKS-style payment switches.

This project demonstrates how to build a **scalable, secure, and resilient banking integration layer** using ASP.NET Core 8.

---

## 🚀 What This Project Solves

In fintech systems, direct integration with payment switches often leads to:

* Tight coupling with external APIs
* Hardcoded encryption logic
* Fragile retry mechanisms
* Poor observability
* Difficult multi-provider support

This connector abstracts all partner-specific complexity behind a clean, stable API.

---

## 🧠 Architecture Overview

```
Core Banking System
        ↓
Bank Connector API (.NET 8)
        ↓
Encryption Layer (RSA / PGP Hybrid)
        ↓
HTTP Resilience Layer (Polly)
        ↓
MEKS Switch (TeamApt-style integration)
```

---

## ⚙️ Key Features

### 🔐 Dual Encryption Support

* RSA + AES-256 hybrid encryption (ISO 20022 style)
* Legacy PGP encryption support
* Pluggable encryption factory pattern

### 🔁 Resilient Transaction Handling

* Polly-based retry & circuit breaker
* Exponential backoff with jitter
* Timeout isolation per request

### 🧾 Idempotent Transaction Design

* Unique 32-character reference generation
* Duplicate-safe transfer handling strategy

### 📡 Clean API Layer

* Thin controllers
* Fully abstracted business logic via `IConnector`
* Partner-agnostic design

### 📊 Response Normalization

* Standardized internal response model
* MEKS/NIP response code mapping

### 🧪 Test Coverage

* Unit tests with xUnit
* Mock-based service isolation
* Encryption round-trip validation
* Reference generation property tests

---

## 📦 API Endpoints

| Method | Endpoint           | Description              |
| ------ | ------------------ | ------------------------ |
| POST   | `/nameinquiry`     | Resolve account name     |
| POST   | `/transfer`        | Initiate transfer        |
| GET    | `/transfer/status` | Check transaction status |

---

## 🔐 Encryption Modes

### RSA Mode (Recommended)

* AES-256-CBC + RSA-OAEP-SHA256 hybrid encryption
* ISO 20022-aligned structure

### PGP Mode (Legacy)

* BouncyCastle-based encryption
* Backward compatibility for older institutions

---

## 🧪 Testing Strategy

* Controller layer tests (routing validation)
* Service layer tests (business logic)
* Encryption round-trip tests
* Logging safety validation
* Deterministic reference generation tests

---

## 🐳 Deployment

* .NET 8 container-ready
* Docker support included
* Key injection via environment variables / mounted secrets
* Kubernetes-friendly design

---

## 📌 Tech Stack

* ASP.NET Core 8
* Polly (Resilience)
* BouncyCastle (Crypto)
* xUnit + Moq
* NLog (Structured Logging)
* Docker

---

## 💡 Design Principles

* Separation of concerns
* Partner abstraction layer
* Fail-safe transaction design
* Secure-by-default encryption handling
* Stateless API design

---

## 📌 Note

This is a **reference implementation** built to demonstrate how secure bank connector systems can be designed for fintech integrations.

---

