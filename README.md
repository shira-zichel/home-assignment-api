# .NET Web API Data Service

A robust .NET Core 9.0 Web API that provides secure data retrieval and management services with role-based authentication, caching, and file storage capabilities.

## Features

- **CRUD Operations**: Create, read, and update data records
- **Role-based Authentication**: JWT-based authentication with user and admin roles
- **Multi-layer Caching**: Redis and in-memory caching with file cache generation
- **MongoDB Integration**: NoSQL database for data persistence
- **RESTful API Design**: Clean and intuitive endpoint structure

## Prerequisites

Before running this project, ensure you have the following installed:

- **.NET 9.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Visual Studio 2022** (Community, Professional, or Enterprise) - [Download here](https://visualstudio.microsoft.com/downloads/)
- **MongoDB** - [Download and install MongoDB Community Server](https://www.mongodb.com/try/download/community)
- **Redis** (optional - for Redis caching) - [Download here](https://redis.io/download) or use Docker
- **Postman** - [Download here](https://www.postman.com/downloads/) for API testing
- **Git** - [Download here](https://git-scm.com/downloads)

## Setup Instructions

### 1. Get the Project Code

#### Option A: Clone the Repository (Recommended)
```bash
git clone [YOUR_REPOSITORY_URL]
cd [YOUR_PROJECT_FOLDER_NAME]
```

#### Option B: Download as ZIP
1. Go to the GitHub repository page
2. Click the green **"Code"** button
3. Select **"Download ZIP"**
4. Extract the ZIP file to your desired location
5. Navigate to the extracted folder

### 2. Open in Visual Studio

1. Launch **Visual Studio 2022**
2. Click **"Open a project or solution"**
3. Navigate to the cloned repository folder
4. Select the `.sln` file and click **"Open"**

### 3. Configure Database and Services

#### MongoDB Setup
1. Start MongoDB service on your machine
2. Ensure MongoDB is running on `localhost:27017` (default port)
3. The application will create the necessary database and collections automatically

#### Redis Setup (Optional)
- If using Redis caching, ensure Redis server is running on `localhost:6379`
- If Redis is not available, the application will fall back to in-memory caching

### 4. Build and Run the Project

1. In Visual Studio, right-click on the solution in **Solution Explorer**
2. Select **"Restore NuGet Packages"** (if prompted)
3. Press **F5** or click **"Start Debugging"** to build and run the project
4. The API will start and open in your default browser (typically at `https://localhost:7059`)
5. Note the base URL displayed - you'll need this for Postman

### 5. Access Swagger Documentation

Once the application is running, you can view the interactive API documentation:

**Swagger URL**: `https://localhost:7059/swagger/index.html`

The Swagger interface provides:
- Complete API endpoint documentation
- Interactive testing capabilities
- Request/response schemas
- Authentication requirements for each endpoint

## API Endpoints

| Method | Endpoint | Description | Authentication Required |
|--------|----------|-------------|------------------------|
| POST | `/api/auth/login` | User authentication | No |
| GET | `/api/data/{id}` | Get data by ID | Yes (User/Admin) |
| POST | `/api/data` | Create new record | Yes (Admin) |
| PUT | `/api/data/{id}` | Update existing record | Yes (Admin) |

## Testing with Postman

### 1. Import Postman Collection

1. Open **Postman**
2. Click **"Import"** button (top left)
3. Select **"Upload Files"**
4. Choose the `[PROJECT_NAME].postman_collection.json` file from the repository
5. Click **"Import"**

### 2. Authentication Flow

#### Step 1: Login
1. Select the **"Login"** request from the imported collection
2. Update the base URL if different from the collection
3. Choose login as either:
   - **User role**: Use provided user credentials
   - **Admin role**: Use provided admin credentials
4. Send the request
5. Copy the JWT token from the response

#### Step 2: Use Protected Endpoints
1. For any other endpoint (Get, Post, Put):
2. Go to the **"Authorization"** tab
3. Select **"Bearer Token"** from the dropdown
4. Paste the JWT token you copied from the login response
5. Send the request

### Sample Authentication Headers
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Project Architecture

- **Caching Strategy**: Multi-tier caching with Redis (primary) and in-memory (fallback)
- **File Storage**: Automatic cache file generation for optimized data retrieval
- **Database**: MongoDB for flexible document storage
- **Security**: JWT-based authentication with role-based authorization

## Troubleshooting

### Common Issues

1. **MongoDB Connection Error**
   - Ensure MongoDB service is running
   - Check connection string in configuration files

2. **Redis Connection Error**
   - Verify Redis server is running (if using Redis caching)
   - Application will automatically fall back to in-memory caching

3. **Build Errors**
   - Restore NuGet packages: Right-click solution → "Restore NuGet Packages"
   - Clean and rebuild: Build → Clean Solution, then Build → Rebuild Solution

4. **JWT Token Expired**
   - Login again to get a fresh token
   - Ensure you're copying the complete token from the login response

## Development Notes

- The application creates cache files automatically during operation
- Database collections are created on first run
- Default ports: API runs on port 7059 (HTTPS)
- Swagger documentation available at `https://localhost:7059/swagger/index.html`

---

**Need Help?** If you encounter any issues setting up or running the project, please check the troubleshooting section above or review the error messages in the Visual Studio output window.
