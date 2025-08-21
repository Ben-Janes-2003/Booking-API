# Booking API

A robust and secure RESTful API for a booking system, built with modern .NET 9 and C#. This project demonstrates a complete, end-to-end feature set, including JWT authentication, role-based authorization, and full unit and integration test coverage. It is designed to be run containerized with Docker and deployed as Infrastructure as Code.

---

## ✨ Features

* **Authentication:** Secure user registration and login using JSON Web Tokens (JWT).
* **Role-Based Access Control:** A two-tier permission system with "User" and "Admin" roles, enforced with endpoint authorization.
* **Booking Management:** Authenticated users can create bookings and view their own booking history.
* **Admin Functionality:** Protected endpoints for administrators to create new, bookable time slots.
* **Secure Production Setup:** A one-time, key-protected endpoint for securely creating the initial admin user in a new environment.
* **Fully Tested:** Comprehensive unit tests (xUnit, Moq) for business logic and integration tests (`WebApplicationFactory`) for security and pipeline validation.
* **Containerized:** Fully configured to run locally with a single Docker Compose command.

---

## 🛠️ Tech Stack

| Category             | Technology                                                                                                    |
| -------------------- | ------------------------------------------------------------------------------------------------------------- |
| **Backend**          | C#, .NET 9, ASP.NET Core                                                                                      |
| **Database**         | PostgreSQL, Entity Framework Core                                                                             |
| **Testing**          | xUnit, Moq, `Microsoft.AspNetCore.Mvc.Testing`                                                                |
| **Containerization** | Docker, Docker Compose                                                                                        |
| **Deployment**       | AWS CDK (Infrastructure as Code), AWS Fargate, Amazon RDS                                                     |

---

## 🚀 Getting Started

### Prerequisites

* .NET 9 SDK
* Docker Desktop

### Running Locally

1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/YourUsername/YourRepoName.git](https://github.com/YourUsername/YourRepoName.git)
    cd YourRepoName
    ```

2.  **Create your environment file:**
    In the root directory, create a `.env` file. This file holds the secrets for your local environment and is ignored by Git. Copy the contents below and replace the placeholder values with your own strong, random keys.

    ```env
    DB_PASSWORD=YourStrongPassword123!
    JWT_KEY=YourSuperLongAndSecretJwtKeyGoesHere
    JWT_ISSUER=https://localhost:8081
    ADMIN_SETUP_KEY=YourSuperSecretAdminSetupKeyGoesHere
    ```

3.  **Run with Docker Compose:**
    Execute the following command from the root of the project to build and start the API and database containers.
    ```bash
    docker-compose up --build
    ```
    The API will be available at `http://localhost:8080`. You can access the interactive documentation at `http://localhost:8080/swagger`.

---

## Endpoints

### Auth Controller (`/api/auth`)

| Method | Route          | Description                                    | Auth Required? |
| :----- | :------------- | :--------------------------------------------- | :------------- |
| `POST` | `/register`    | Registers a new user with the "User" role.     | No             |
| `POST` | `/login`       | Logs in a user and returns a JWT.              | No             |
| `POST` | `/setup-admin` | Creates the first admin user (one-time use).   | Secret Key     |

### Slots Controller (`/api/slots`)

| Method | Route          | Description                                    | Auth Required? |
| :----- | :------------- | :--------------------------------------------- | :------------- |
| `GET`  | `/`            | Gets a list of all available, unbooked slots.  | No             |
| `POST` | `/`            | Creates a new time slot.                       | Admin Only     |

### Bookings Controller (`/api/bookings`)

| Method | Route          | Description                                    | Auth Required? |
| :----- | :------------- | :--------------------------------------------- | :------------- |
| `POST` | `/`            | Creates a new booking for the logged-in user.  | User           |
| `GET`  | `/{id}`        | Gets a specific booking by ID for the user.    | User           |
| `GET`  | `/my-bookings` | Gets all bookings for the logged-in user.      | User           |
