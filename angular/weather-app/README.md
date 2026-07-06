# Angular Weather App

A modern, high-performance Weather Application built with Angular. This project follows strict, corporate-style "Azure/Microsoft Fluent" design principles, prioritizing sharp corners, structural borders, and high-contrast styling for a premium feel.

## ✨ Features

- **Modern Angular Reactivity:** Built utilizing Angular's modern `Signal` architecture for seamless state management.
- **Dynamic Weather Table:** Displays 5-day weather forecasts including formatted dates, temperatures (°C & °F), and summaries.
- **Hot Weather Alerts:** Automatically highlights rows in a subtle warm yellow (`#fff4ce`) when temperatures exceed 30°C.
- **Live Data Interaction:** Features a header action bar that calculates total records in real-time and includes a manual **Refresh** button to pull fresh data from the API endpoint.
- **Premium Azure Aesthetics:** A sharp, 0px border-radius design system utilizing specific Microsoft-inspired gray scales (`#f3f2f1`) and Azure blue (`#0078d4`) styling.
- **Environment Configuration:** Securely utilizes Angular's native `environment.ts` architecture for managing external API endpoints.

## 🛠️ Tech Stack

- **Framework:** Angular 17+ (Zoneless, Signals)
- **Styling:** Vanilla CSS (Azure Fluent Design)
- **Testing:** Vitest (JSDOM environment)
- **CI/CD:** GitHub Actions

## 🚀 Getting Started Locally

### Prerequisites
- Node.js (v20 or v22)
- npm (Node Package Manager)

### Installation
1. Clone the repository and navigate to the project root.
2. Install dependencies:
   ```bash
   npm install
   ```
3. Start the development server:
   ```bash
   npm run start
   ```
   Navigate to `http://localhost:4200/`. The application will automatically reload if you change any of the source files.

## 🧪 Running Unit Tests

This project utilizes **Vitest** for blazingly fast, headless testing without requiring cumbersome browser drivers.

To execute tests:
```bash
npm test
```
*(Tests run natively in Node using JSDOM for a streamlined developer experience).*

## 🚢 CI/CD & GitHub Pages Deployment

The repository is configured with a fully automated CI/CD pipeline using **GitHub Actions**.

### Workflow Pipeline (`.github/workflows/angular-ci-cd.yml`)
Every push to the `main` branch triggers the following workflow:
1. **Setup & Install:** Provisions a Node 22 environment and securely caches dependencies.
2. **Build:** Compiles the application for production using `npm run build -- --base-href /genspark-training/` to ensure assets load correctly on GitHub Pages subpaths.
3. **Unit Tests:** Executes Vitest unit tests in continuous integration mode (`--watch=false`).
4. **Smoke Test:** Boots a temporary HTTP server and pings the built application to verify the build isn't crashing.
5. **Deployment:** Automatically packages the `dist` folder and deploys it live to the repository's GitHub Pages environment.

### Live URL
The application is continuously deployed and can be viewed via the repository's GitHub Pages link. (e.g., `https://<username>.github.io/genspark-training/`).
