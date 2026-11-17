# Eventloop Azure Production Deployment Guide

This guide provides step-by-step instructions to provision the required Azure resources using cost-friendly (or free) tiers, configure GitHub Secrets for automated CI/CD, and deploy the entire Eventloop platform.

---

## 1. Resource Architecture & Cost-Friendly Tiers

All resources should be placed in a single resource group (e.g., `rg-eventloop-prod`) in your preferred Azure region (e.g., `East US` or `West Europe`).

| # | Resource Type | Recommended Name | SKU / Tier | Estimated Cost |
|---|---|---|---|---|
| 1 | **Resource Group** | `rg-eventloop-prod` | N/A | Free |
| 2 | **Azure SQL Database** | `sqldb-eventloop-prod` (Server: `sql-eventloop-prod`) | **Basic** (5 DTUs, 2 GB) OR **General Purpose Serverless** (0.5 vCore, auto-pause after 1 hour) | ~\$5.00 / month (Basic) or pennies (Serverless auto-paused) |
| 3 | **Storage Account** | `steventloopprod` | **Standard LRS** (General Purpose v2) | Pay-as-you-go (~few cents/month) |
| 4 | **App Service Plan & Web App** | `asp-eventloop-prod` & `app-eventloop-api-prod` | **Linux Basic B1** (or Free F1 for testing) | Free (F1) or ~\$13 / month (B1) |
| 5 | **Function App** | `func-eventloop-jobs-prod` | **Consumption (Serverless Y1)** (.NET 8 Isolated) | **Free** (First 1,000,000 executions/month free) |
| 6 | **Static Web App** | `swa-eventloop-web-prod` | **Free Tier** | **Free** |
| 7 | **Application Insights** | `appi-eventloop-prod` | Workspace-based (Pay-as-you-go) | **Free** (First 5 GB data ingestion/month free) |
| 8 | *(Optional)* **Communication Services** | `acs-eventloop-prod` | Pay-as-you-go Email | Free / fractions of a cent per email |

---

## 2. Step-by-Step Portal Setup Instructions

### Step 2.1: Create Resource Group
1. Sign in to the [Azure Portal](https://portal.azure.com).
2. Search for **Resource groups** $\rightarrow$ click **Create**.
3. Name: `rg-eventloop-prod`.
4. Region: Choose your preferred region (e.g., `East US`).
5. Click **Review + create** $\rightarrow$ **Create**.

---

### Step 2.2: Create Azure SQL Server & Database
1. In the Azure Portal, search for **SQL databases** $\rightarrow$ click **Create**.
2. **Resource group**: `rg-eventloop-prod`.
3. **Database name**: `sqldb-eventloop-prod`.
4. **Server**: Click **Create new**:
   - Server name: `sql-eventloop-prod-[your-unique-suffix]`
   - Location: Same region as resource group.
   - Authentication method: **Use SQL authentication**.
   - Server admin login: `eventloopadmin`.
   - Password: `YourStrongPassword!123`.
5. **Compute + storage**: Click **Configure database**:
   - Select **Basic** (5 DTUs, 2 GB max size) for ~\$5/month, OR select **General Purpose (Serverless)** with 0.5 minimum vCores and enable **Auto-pause delay** (e.g. 1 hour).
6. **Networking tab**:
   - Connectivity method: **Public endpoint**.
   - Firewall rules: Set **Allow Azure services and resources to access this server** to **Yes**.
   - Add your current client IP address if you wish to query the database from SQL Server Management Studio (SSMS) or Azure Data Studio.
7. Click **Review + create** $\rightarrow$ **Create**.
8. Once created, navigate to the database $\rightarrow$ **Connection strings** $\rightarrow$ copy the **ADO.NET** connection string. Replace `{your_password}` with your SQL admin password.

---

### Step 2.3: Create Storage Account & Queue
1. Search for **Storage accounts** $\rightarrow$ click **Create**.
2. **Resource group**: `rg-eventloop-prod`.
3. **Storage account name**: `steventloop[uniquesuffix]` (must be lowercase alphanumeric, 3-24 chars).
4. **Performance**: **Standard**, Redundancy: **Locally-redundant storage (LRS)**.
5. Click **Review + create** $\rightarrow$ **Create**.
6. Once deployed:
   - In the left sidebar, click **Queues** (under Data storage).
   - Click **+ Queue**, name it: `registration-events`, click **OK**.
   - In the left sidebar, click **Access keys** $\rightarrow$ copy **Connection string** (Key 1).

---

### Step 2.4: Create Application Insights
1. Search for **Application Insights** $\rightarrow$ click **Create**.
2. **Resource group**: `rg-eventloop-prod`.
3. **Name**: `appi-eventloop-prod`.
4. Click **Review + create** $\rightarrow$ **Create**.
5. Once created, copy the **Connection String** displayed in the Overview pane.

---

### Step 2.5: Create Azure App Service (Web API)
1. Search for **App Services** $\rightarrow$ click **Create** $\rightarrow$ **Web App**.
2. **Resource group**: `rg-eventloop-prod`.
3. **Name**: `app-eventloop-api-[uniquesuffix]`.
4. **Publish**: Code.
5. **Runtime stack**: **.NET 8 (LTS)**.
6. **Operating System**: **Linux**.
7. **Pricing plan**: Select **Basic B1** (or Free F1 for trial).
8. **Monitoring tab**: Set **Enable Application Insights** to **Yes** and pick `appi-eventloop-prod`.
9. Click **Review + create** $\rightarrow$ **Create**.
10. Once deployed:
    - Go to **Configuration** (or **Environment variables**) under Settings $\rightarrow$ add:
      - `ConnectionStrings__DefaultConnection`: *<Azure SQL ADO.NET connection string>*
      - `AzureStorage__ConnectionString`: *<Storage Account connection string>*
      - `AzureStorage__QueueName`: `registration-events`
      - `Jwt__SecretKey`: *<A secure random string of at least 32 characters>*
      - `Jwt__Issuer`: `EventloopApi`
      - `Jwt__Audience`: `EventloopApp`
      - `Cors__AllowedOrigins__0`: `https://<your-static-web-app-url>.azurestaticapps.net`
    - Click **Save**.
    - Under the **Overview** blade, click **Get publish profile** and download the XML file.

---

### Step 2.6: Create Azure Function App (Isolated Worker)
1. Search for **Function App** $\rightarrow$ click **Create**.
2. **Hosting option**: **Consumption** (Serverless).
3. **Resource group**: `rg-eventloop-prod`.
4. **Function App name**: `func-eventloop-jobs-[uniquesuffix]`.
5. **Runtime stack**: **.NET**, Version: **8 Isolated**.
6. **Operating System**: **Linux**.
7. **Storage tab**: Select the storage account created earlier (`steventloop[uniquesuffix]`).
8. **Monitoring tab**: Select `appi-eventloop-prod`.
9. Click **Review + create** $\rightarrow$ **Create**.
10. Once deployed:
    - Go to **Configuration** (or **Environment variables**) under Settings $\rightarrow$ add:
      - `AzureWebJobsStorage`: *<Storage Account connection string>*
      - `DefaultConnection`: *<Azure SQL connection string>*
      - `Email__Provider`: `Console` (or `AzureCommunicationServices`)
    - Click **Save**.
    - In the **Overview** blade, click **Get publish profile** and download the XML file.

---

### Step 2.7: Create Azure Static Web Apps (Frontend)
1. Search for **Static Web Apps** $\rightarrow$ click **Create**.
2. **Resource group**: `rg-eventloop-prod`.
3. **Name**: `swa-eventloop-web-prod`.
4. **Plan type**: **Free**.
5. **Deployment details**: Select **Other** (since GitHub Actions will push the pre-built `dist` assets using the API token).
6. Click **Review + create** $\rightarrow$ **Create**.
7. Once created:
   - In the **Overview** blade, copy the **URL** (e.g. `https://salmon-sea-0a1b2c3d4.azurestaticapps.net`).
   - Click **Manage deployment token** and copy the secret token.
   - Note: Update the App Service CORS configuration (`Cors__AllowedOrigins__0`) with this Static Web App URL so browser requests are permitted!

---

## 3. GitHub Secrets Configuration

In your GitHub repository, navigate to **Settings** $\rightarrow$ **Secrets and variables** $\rightarrow$ **Actions** $\rightarrow$ click **New repository secret** for each item:

| Secret Name | Value Description | Where to Get It |
|---|---|---|
| `AZURE_SQL_CONNECTION_STRING` | Complete ADO.NET connection string to Azure SQL Database | Azure Portal $\rightarrow$ SQL Database $\rightarrow$ Connection strings |
| `AZURE_WEBAPP_NAME` | Name of your API App Service (e.g. `app-eventloop-api-prod`) | Azure Portal $\rightarrow$ Web App Overview |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Entire contents of downloaded publish profile XML for API | Azure Portal $\rightarrow$ Web App $\rightarrow$ "Get publish profile" |
| `AZURE_FUNCTIONAPP_NAME` | Name of your Function App (e.g. `func-eventloop-jobs-prod`) | Azure Portal $\rightarrow$ Function App Overview |
| `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` | Entire contents of downloaded publish profile XML for Functions | Azure Portal $\rightarrow$ Function App $\rightarrow$ "Get publish profile" |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Deployment token for Azure Static Web Apps | Azure Portal $\rightarrow$ Static Web App $\rightarrow$ "Manage deployment token" |
| `VITE_API_URL` | Public URL of your Azure App Service API (e.g. `https://app-eventloop-api-prod.azurewebsites.net`) | Azure Portal $\rightarrow$ App Service Overview $\rightarrow$ Default domain |

---

## 4. How the CI/CD Pipeline Deploys

When a commit is pushed to `main` / `master`:
1. **CI Validation** (`ci.yml`):
   - Restores, builds, and runs xUnit tests on backend with code coverage.
   - Restores, lints (`tsc --noEmit`), and runs Vitest tests on the frontend.
2. **Database Migration** (`deploy.yml`):
   - Generates an idempotent EF Core migration bundle executable (`efbundle`).
   - Runs `./efbundle` directly against Azure SQL. If migrations were already applied, it completes in milliseconds without data loss.
3. **API Deployment**:
   - Publishes `Eventloop.Api` in `Release` configuration and deploys to Azure App Service via `azure/webapps-deploy@v3`.
4. **Functions Deployment**:
   - Publishes `Eventloop.Functions` in `Release` configuration and deploys to Azure Function App via `Azure/functions-action@v1`.
5. **Frontend Deployment**:
   - Builds production Vite assets with `VITE_API_URL` baked in, then uploads to Azure Static Web Apps via `Azure/static-web-apps-deploy@v1`.

---

## 5. Reviewer Demo Data Initialization

Once deployed, you can initialize the live database with sample organizers, students, full events, waitlists, and check-in attendance data:

- **Via HTTP**:
  Send a `POST` request to `https://<your-app-service-url>/api/demo/seed` (or open Swagger at `/swagger` and execute the `/api/demo/seed` endpoint).
- **Via Frontend UI**:
  Open the frontend URL, go to the **Login** screen, and click **Reset / Seed Demo DB**.
