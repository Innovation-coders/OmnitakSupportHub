# Omnitak Support Hub

## 📌 Project Overview

Omnitak Support Hub is a **web-based IT Support Ticket Management System** built with **ASP.NET Core MVC**. It enables organizations to manage support requests efficiently by providing dashboards for different user roles (Admin, Manager, Agent, End User). The system also supports real-time communication, knowledge sharing, and effective workflow management.

## 🚀 Features

* **User Authentication & Authorization** (custom Auth service with API support).
* **Role-based Dashboards**:

  * Admin Dashboard
  * Manager Dashboard
  * Support Agent Dashboard
  * End User Dashboard
* **Ticket Management** (create, assign, update, resolve, close).
* **Team-based Routing** (IT Support, Network Team, Security, Application Support).
* **Chat System** between users and support agents.
* **Knowledge Base & Feedback System**.
* **Audit Logging** for tracking activities.
* **Global Exception Handling & Logging Middleware**.
* **Database Integration** with SQL Server (`OmnitakITSupportDB.sql`).

## 🛠️ Tech Stack

* **Backend**: ASP.NET Core MVC (.NET 9.0)
* **Database**: Microsoft SQL Server
* **ORM**: Entity Framework Core
* **Authentication**: Custom JWT + Cookie Authentication
* **Frontend**: Razor Views, Bootstrap, JavaScript
* **Logging**: Serilog (with middleware integration)

## 📂 Project Structure

```
OmnitakSupportHub/
├── Controllers/        # MVC & API controllers (Tickets, Users, Auth, etc.)
├── Models/             # Entity models (Ticket, User, AuditLog, etc.)
├── Middleware/         # Global exception handling
├── Database/           # SQL scripts (OmnitakITSupportDB.sql)
├── Views/              # Razor view files for UI
├── Program.cs          # Entry point
├── appsettings.json    # Configurations
```

## 👥 Team Collaboration

* Team of **4 developers**.
* Followed **Agile methodology**:

  * **Daily stand-ups** to track progress.
  * **Sprint-based planning & reviews**.
  * **Pull Requests** for collaboration and code reviews.
* Ensured teamwork, transparency, and iterative improvements.

## ⚡ Getting Started

### Prerequisites

* [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
* [SQL Server](https://www.microsoft.com/en-us/sql-server)
* Visual Studio / VS Code

### Installation & Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/your-repo/OmnitakSupportHub.git
   cd OmnitakSupportHub
   ```

2. **Configure Database**

   * Update `appsettings.json` with your SQL Server connection string.
   * Run `OmnitakITSupportDB.sql` to set up the database.

3. **Run the project**

   ```bash
   dotnet build
   dotnet run
   ```

4. **Access the app**

   * Open browser: `https://localhost:5001`

## 🧪 Testing

* REST APIs can be tested via **Swagger** or **Postman**.
* Includes **xUnit tests** for controllers and services.

## 📈 Future Improvements

* SignalR integration for real-time notifications.
* Automated email notifications for ticket updates.
* Improved analytics & reporting dashboards.

## 🤝 Contributors

* **Team of 4 Agile Developers**

  * Implemented PR-based workflow
  * Conducted daily stand-ups
  * Iteratively delivered working features

---

✅ *This project demonstrates strong teamwork, Agile collaboration, and technical implementation of a full-stack ASP.NET Core support system.*
