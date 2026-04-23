# 🚀 ZATCA E-Invoicing Phase 2 - .NET Implementation

A production-ready **ASP.NET Core** Web API implementation that fulfills the technical requirements for **ZATCA (FATOORA)** Phase 2 (Integration Phase) in Saudi Arabia. This project demonstrates high-level data integrity using cryptographic chaining and compliant QR code generation.

---

## 🛠️ Key Technical Features

### 🔐 Cryptographic Hash Chaining
* **Sequential Linking**: Each invoice is cryptographically linked to its predecessor using the **SHA-256** hashing algorithm.
* **Tamper Detection**: The system includes an integrity engine that detects any manual unauthorized modifications in the SQL database.
* **Root of Trust**: The sequence is initialized with a "0" hash for the first invoice, ensuring a standardized starting point.

### 📱 Compliant QR Code Generation (TLV)
* **Standardized Encoding**: Generates QR codes using **TLV (Tag-Length-Value)** encoding as strictly mandated by ZATCA.
* **Comprehensive Data**: Encodes Seller Name, VAT Number, Timestamp, Invoice Totals, and the unique Invoice Hash.
* **Base64 Delivery**: Converts the TLV binary data into a Base64 string for seamless frontend integration and scanning.

### 🛡️ Data Integrity & Protection
* **Integrity Validator**: Features a dedicated `/api/Invoice/validate-chain` endpoint to audit and verify the entire history of transactions.
* **Soft Delete Mechanism**: Implements an `IsDeleted` flag to ensure that no record deletion can break the cryptographic chain.
* **Consistent Formatting**: Utilizes strict ISO 8601 date formatting and fixed-point decimal precision (`F2`) to maintain hash consistency across different environments.

---

## 💻 Tech Stack

* **Framework**: .NET 10 (C#)
* **Database**: Microsoft SQL Server
* **ORM**: Entity Framework Core
* **Security**: System.Security.Cryptography (SHA-256)
* **Documentation**: Swagger / OpenAPI

---

## 📂 Database Schema Overview

| Column | Type | Description |
| :--- | :--- | :--- |
| `UUID` | `UniqueIdentifier` | Universally Unique Identifier for ZATCA compliance |
| `PreviousInvoiceHash` | `NVARCHAR(MAX)` | The hash value retrieved from the preceding invoice |
| `CurrentHash` | `NVARCHAR(MAX)` | The digital fingerprint of the current invoice data |
| `QRCode` | `NVARCHAR(MAX)` | Base64 string of the TLV-encoded invoice data |
| `IsDeleted` | `BIT` | Protection flag to maintain chain continuity |

---

## 🚦 Getting Started

1. **Clone the Repository**:
   ```bash
   git clone [https://github.com/your-username/ZatcaInvoicingApp.git](https://github.com/your-username/ZatcaInvoicingApp.git)
