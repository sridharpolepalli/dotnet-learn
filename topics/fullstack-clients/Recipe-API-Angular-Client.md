# Step-by-Step: Angular Client for Recipe Web API

**Detailed guide** to set up, implement, and run an **Angular** client that consumes the **Recipe Web API** (Categories, and optionally Recipes). Covers Angular CLI setup, project structure, environment configuration, models, HTTP service, components, routing, and running with the API.

---

## Table of Contents

| Step | Topic |
|------|--------|
| 0 | [Prerequisites](#step-0-prerequisites) |
| 1 | [Create Angular project](#step-1-create-angular-project) |
| 2 | [Project structure and key files](#step-2-project-structure-and-key-files) |
| 3 | [Configure API base URL (environment)](#step-3-configure-api-base-url-environment) |
| 4 | [Define models (interfaces)](#step-4-define-models-interfaces) |
| 5 | [Create API service (HttpClient)](#step-5-create-api-service-httpclient) |
| 6 | [Category list component](#step-6-category-list-component) |
| 7 | [Category detail and form components](#step-7-category-detail-and-form-components) |
| 8 | [Routing and navigation](#step-8-routing-and-navigation) |
| 9 | [App component and module setup](#step-9-app-component-and-module-setup) |
| 10 | [Run and test](#step-10-run-and-test) |
| 11 | [CORS and proxy (optional)](#step-11-cors-and-proxy-optional) |

---

## Step 0: Prerequisites

Before starting, ensure you have:

| Requirement | How to check |
|-------------|----------------|
| **Node.js** (LTS, e.g. 18.x or 20.x) | `node -v` |
| **npm** (comes with Node) | `npm -v` |
| **Angular CLI** | `ng version` or install: `npm install -g @angular/cli@18` (use a version compatible with your Node) |

**Install Angular CLI (if needed):**

```bash
npm install -g @angular/cli@18
```

**Recipe Web API** must be running (e.g. from Visual Studio or `dotnet run` in the API project). Default base URL used in this guide: **https://localhost:7071** (adjust to match your API).

---

## Step 1: Create Angular project

### 1.1 Create a new workspace and app

Open a terminal in a folder where you want the client (e.g. alongside your Recipe API solution).

```bash
ng new recipe-client --routing --style=css --ssr=false
```

- **--routing** — Adds Angular Router.
- **--style=css** — Use CSS for styles (use `scss` if you prefer).
- **--ssr=false** — Client-side only (no server-side rendering). Use `--ssr=true` if you want SSR.

When prompted **"Would you like to add Angular routing?"** — Yes (already added with `--routing`).  
When prompted **"Which stylesheet format?"** — CSS (or SCSS).

### 1.2 Go into the project

```bash
cd recipe-client
```

### 1.3 Verify the app runs

```bash
ng serve
```

Open **http://localhost:4200** in the browser. You should see the default Angular welcome page. Stop the server with **Ctrl+C** when done.

---

## Step 2: Project structure and key files

After creation, the project looks like this (simplified):

```
recipe-client/
├── src/
│   ├── app/
│   │   ├── app.component.ts
│   │   ├── app.component.html
│   │   ├── app.config.ts          (standalone app: config)
│   │   └── app.routes.ts          (routing)
│   ├── index.html
│   ├── main.ts
│   └── styles.css
├── angular.json
├── package.json
└── tsconfig.json
```

- **app.config.ts** — Configures providers (e.g. HttpClient, services).
- **app.routes.ts** — Route definitions.
- **angular.json** — Build and serve settings; we will use **proxy** here if needed for API calls.

---

## Step 3: Configure API base URL (environment)

So the app knows where the Recipe API is, use **environment** files.

### 3.1 Create environment files

**src/environments/environment.ts** (development):

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7071/api'
};
```

**src/environments/environment.development.ts** (optional; Angular uses this for `ng serve`):

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7071/api'
};
```

**src/environments/environment.prod.ts** (production):

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://your-api-server.com/api'
};
```

### 3.2 Register environment in angular.json (if not already)

In **angular.json**, under **projects → recipe-client → architect → build → options**:

```json
"fileReplacements": [
  {
    "replace": "src/environments/environment.ts",
    "with": "src/environments/environment.development.ts"
  }
]
```

And under **build → configurations → production**:

```json
"fileReplacements": [
  {
    "replace": "src/environments/environment.ts",
    "with": "src/environments/environment.prod.ts"
  }
]
```

If you use a single **environment.ts** for dev, you can skip `fileReplacements` and just set `apiUrl` in that file.

---

## Step 4: Define models (interfaces)

Create TypeScript interfaces that match the API DTOs.

### 4.1 Create a folder and files

```bash
mkdir -p src/app/models
```

**src/app/models/category.model.ts:**

```typescript
export interface Category {
  id: number;
  name: string;
}
```

**src/app/models/recipe.model.ts** (optional; for future Recipe CRUD):

```typescript
export interface Recipe {
  id: number;
  name: string;
  categoryId: number;
  ingredientJson?: string;
  instructions?: string;
}
```

---

## Step 5: Create API service (HttpClient)

A service will call the Recipe API using **HttpClient**.

### 5.1 Generate the service

```bash
ng generate service services/category --skip-tests
```

This creates **src/app/services/category.service.ts** (and possibly **category.service.spec.ts** if you don’t use `--skip-tests`).

### 5.2 Provide HttpClient

In **src/app/app.config.ts**, add `provideHttpClient()`:

```typescript
import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient()
  ]
};
```

### 5.3 Implement CategoryService

**src/app/services/category.service.ts:**

```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { Category } from '../models/category.model';

@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  private apiUrl = `${environment.apiUrl}/categories`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Category[]> {
    return this.http.get<Category[]>(this.apiUrl).pipe(
      catchError(() => of([]))
    );
  }

  getById(id: number): Observable<Category | null> {
    return this.http.get<Category>(`${this.apiUrl}/${id}`).pipe(
      catchError(() => of(null))
    );
  }

  create(category: Category): Observable<Category> {
    return this.http.post<Category>(this.apiUrl, category);
  }

  update(id: number, category: Category): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, category);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
```

- **providedIn: 'root'** — Single instance for the app (no need to add to a module).
- **catchError** — Returns empty array or null on error so the UI doesn’t break; you can add logging or user feedback later.

---

## Step 6: Category list component

### 6.1 Generate the component

```bash
ng generate component components/category-list --skip-tests
```

### 6.2 Implement the list component

**src/app/components/category-list/category-list.component.ts:**

```typescript
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CategoryService } from '../../services/category.service';
import { Category } from '../../models/category.model';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './category-list.component.html',
  styleUrl: './category-list.component.css'
})
export class CategoryListComponent implements OnInit {
  categories: Category[] = [];
  loading = true;
  error: string | null = null;

  constructor(private categoryService: CategoryService) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading = true;
    this.error = null;
    this.categoryService.getAll().subscribe({
      next: (data) => {
        this.categories = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load categories. Is the API running?';
        this.loading = false;
        console.error(err);
      }
    });
  }

  delete(id: number): void {
    if (!confirm('Delete this category?')) return;
    this.categoryService.delete(id).subscribe({
      next: () => this.loadCategories(),
      error: (err) => {
        this.error = 'Failed to delete';
        console.error(err);
      }
    });
  }
}
```

**src/app/components/category-list/category-list.component.html:**

```html
<div class="category-list">
  <h1>Categories</h1>

  <p *ngIf="loading">Loading...</p>
  <p *ngIf="error" class="error">{{ error }}</p>

  <a routerLink="/categories/new">Add Category</a>

  <table *ngIf="!loading && !error && categories.length > 0">
    <thead>
      <tr>
        <th>Id</th>
        <th>Name</th>
        <th></th>
      </tr>
    </thead>
    <tbody>
      <tr *ngFor="let cat of categories">
        <td>{{ cat.id }}</td>
        <td><a [routerLink]="['/categories', cat.id]">{{ cat.name }}</a></td>
        <td>
          <button (click)="delete(cat.id)">Delete</button>
        </td>
      </tr>
    </tbody>
  </table>

  <p *ngIf="!loading && !error && categories.length === 0">No categories yet. Add one.</p>
</div>
```

**src/app/components/category-list/category-list.component.css** (optional):

```css
.category-list { padding: 1rem; }
.error { color: red; }
table { border-collapse: collapse; margin-top: 1rem; }
th, td { border: 1px solid #ccc; padding: 0.5rem 1rem; }
a { margin-right: 1rem; }
```

---

## Step 7: Category detail and form components

### 7.1 Category detail (view one)

```bash
ng generate component components/category-detail --skip-tests
```

**src/app/components/category-detail/category-detail.component.ts:**

```typescript
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CategoryService } from '../../services/category.service';
import { Category } from '../../models/category.model';

@Component({
  selector: 'app-category-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './category-detail.component.html',
  styleUrl: './category-detail.component.css'
})
export class CategoryDetailComponent implements OnInit {
  category: Category | null = null;
  loading = true;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private categoryService: CategoryService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (isNaN(id)) {
      this.router.navigate(['/categories']);
      return;
    }
    this.categoryService.getById(id).subscribe({
      next: (data) => {
        this.category = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.router.navigate(['/categories']);
      }
    });
  }
}
```

**src/app/components/category-detail/category-detail.component.html:**

```html
<div *ngIf="loading">Loading...</div>
<div *ngIf="!loading && category">
  <h1>{{ category.name }}</h1>
  <p>Id: {{ category.id }}</p>
  <a routerLink="/categories">Back to list</a>
  <a [routerLink]="['/categories', category.id, 'edit']">Edit</a>
</div>
<div *ngIf="!loading && !category">Not found.</div>
```

### 7.2 Category form (add / edit)

```bash
ng generate component components/category-form --skip-tests
```

**src/app/components/category-form/category-form.component.ts:**

```typescript
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CategoryService } from '../../services/category.service';
import { Category } from '../../models/category.model';

@Component({
  selector: 'app-category-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './category-form.component.html',
  styleUrl: './category-form.component.css'
})
export class CategoryFormComponent implements OnInit {
  form: FormGroup;
  isEdit = false;
  id: number | null = null;
  submitting = false;
  error: string | null = null;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private categoryService: CategoryService
  ) {
    this.form = this.fb.group({
      id: [0, Validators.required],
      name: ['', [Validators.required, Validators.maxLength(100)]]
    });
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam && idParam !== 'new') {
      this.isEdit = true;
      this.id = Number(idParam);
      this.categoryService.getById(this.id).subscribe({
        next: (cat) => {
          if (cat) this.form.patchValue(cat);
        },
        error: () => this.router.navigate(['/categories'])
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    this.error = null;
    const value = this.form.value as Category;

    const req = this.isEdit && this.id
      ? this.categoryService.update(this.id, value)
      : this.categoryService.create(value);

    req.subscribe({
      next: () => this.router.navigate(['/categories']),
      error: (err) => {
        this.error = err?.error?.message || 'Request failed';
        this.submitting = false;
      }
    });
  }
}
```

**src/app/components/category-form/category-form.component.html:**

```html
<div class="form">
  <h1>{{ isEdit ? 'Edit' : 'New' }} Category</h1>
  <p *ngIf="error" class="error">{{ error }}</p>

  <form [formGroup]="form" (ngSubmit)="onSubmit()">
    <div>
      <label for="id">Id</label>
      <input id="id" type="number" formControlName="id" [readonly]="isEdit" />
    </div>
    <div>
      <label for="name">Name</label>
      <input id="name" type="text" formControlName="name" />
      <span *ngIf="form.get('name')?.invalid && form.get('name')?.touched">Name is required.</span>
    </div>
    <button type="submit" [disabled]="form.invalid || submitting">
      {{ submitting ? 'Saving...' : (isEdit ? 'Update' : 'Create') }}
    </button>
    <a routerLink="/categories">Cancel</a>
  </form>
</div>
```

---

## Step 8: Routing and navigation

### 8.1 Define routes

**src/app/app.routes.ts:**

```typescript
import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/categories', pathMatch: 'full' },
  { path: 'categories', loadComponent: () => import('./components/category-list/category-list.component').then(m => m.CategoryListComponent) },
  { path: 'categories/new', loadComponent: () => import('./components/category-form/category-form.component').then(m => m.CategoryFormComponent) },
  { path: 'categories/:id', loadComponent: () => import('./components/category-detail/category-detail.component').then(m => m.CategoryDetailComponent) },
  { path: 'categories/:id/edit', loadComponent: () => import('./components/category-form/category-form.component').then(m => m.CategoryFormComponent) },
  { path: '**', redirectTo: '/categories' }
];
```

- **loadComponent** — Lazy-loads each component (standalone).
- **categories/new** — Add new category.
- **categories/:id** — View one category.
- **categories/:id/edit** — Edit category.

### 8.2 Add router outlet and nav in App component

**src/app/app.component.ts:**

```typescript
import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <nav>
      <a routerLink="/categories" routerLinkActive="active">Categories</a>
    </nav>
    <main>
      <router-outlet></router-outlet>
    </main>
  `,
  styles: [`
    nav { padding: 0.5rem 1rem; background: #f0f0f0; }
    nav a { margin-right: 1rem; }
    nav a.active { font-weight: bold; }
    main { padding: 1rem; }
  `]
})
export class AppComponent {}
```

---

## Step 9: App component and module setup

- **app.config.ts** — Already updated with `provideHttpClient()` and `provideRouter(routes)`.
- **main.ts** — Should bootstrap the app with `AppComponent` and `appConfig`; no change needed unless you use a different bootstrap.
- All components used in routes are **standalone** and lazy-loaded, so no NgModule is required for them.

**main.ts** (typical content):

```typescript
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
```

---

## Step 10: Run and test

### 10.1 Start the Recipe API

From the Recipe Web API project folder:

```bash
dotnet run
```

Or run from Visual Studio (F5). Note the URL (e.g. **https://localhost:7071**). Ensure **environment.apiUrl** in the Angular app points to **https://localhost:7071/api**.

### 10.2 Start the Angular app

From the **recipe-client** folder:

```bash
ng serve
```

Open **http://localhost:4200**. You should see the nav and the Categories list.

### 10.3 Test flows

| Action | Steps |
|--------|--------|
| **List** | Open http://localhost:4200/categories — should call GET /api/categories. |
| **Add** | Click “Add Category”, fill Id and Name, submit — POST /api/categories. |
| **View** | Click a category name — GET /api/categories/{id}. |
| **Edit** | Click “Edit”, change name, submit — PUT /api/categories/{id}. |
| **Delete** | Click “Delete” on the list, confirm — DELETE /api/categories/{id}. |

If the API is not running or the URL is wrong, you’ll see “Failed to load categories” (or the error message you set in the list component).

### 10.4 Build for production

```bash
ng build --configuration=production
```

Output is in **dist/recipe-client**. Serve that folder with any web server or host it on Azure/AWS/etc. Set **environment.prod.ts** `apiUrl` to your production API URL.

---

## Step 11: CORS and proxy (optional)

### 11.1 CORS (API side)

If the API runs on **https://localhost:7071** and the Angular app on **http://localhost:4200**, the browser enforces **CORS**. The API must allow the Angular origin.

In the **Recipe API** **Program.cs**:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ... after app = builder.Build()
app.UseCors();
```

Place **UseCors()** before **UseAuthorization()** and **MapControllers()**.

### 11.2 Proxy (Angular dev server, alternative to CORS)

If you don’t want to enable CORS on the API during development, proxy API requests through the Angular dev server.

**Create proxy.conf.json** in the **recipe-client** root:

```json
{
  "/api": {
    "target": "https://localhost:7071",
    "secure": false
  }
}
```

**Use the proxy in angular.json** — under **projects → recipe-client → architect → serve → options**:

```json
"options": {
  "proxyConfig": "proxy.conf.json"
}
```

**Change environment to use relative URL** (so requests go to the same host: port 4200, then proxied to 7071):

**src/environments/environment.ts** (and environment.development.ts):

```typescript
export const environment = {
  production: false,
  apiUrl: '/api'
};
```

Restart **ng serve**. Requests to **http://localhost:4200/api/categories** will be proxied to **https://localhost:7071/api/categories**.

---

## Summary checklist

| Step | What you did |
|------|----------------|
| 0 | Installed Node, npm, Angular CLI. |
| 1 | Created `recipe-client` with routing and CSS. |
| 2 | Understood project structure (app.config, app.routes, angular.json). |
| 3 | Added environment files and `apiUrl` (and optional fileReplacements). |
| 4 | Created `Category` (and optionally `Recipe`) interfaces under `app/models`. |
| 5 | Created `CategoryService` with HttpClient; registered `provideHttpClient()`. |
| 6 | Built category-list component (list, delete, link to add/detail). |
| 7 | Built category-detail and category-form (add/edit) with reactive form. |
| 8 | Defined routes and added nav + router-outlet in App component. |
| 9 | Confirmed app.config and main.ts. |
| 10 | Started API and `ng serve`; tested list, add, view, edit, delete. |
| 11 | (Optional) Configured CORS on API or proxy in Angular for local dev. |

---

## References

- [Angular CLI](https://angular.dev/tools/cli)
- [Angular HttpClient](https://angular.dev/guide/http)
- [Angular Router](https://angular.dev/guide/routing)
- [Angular Standalone Components](https://angular.dev/guide/components/standalone)
- [Proxy to backend (Angular)](https://angular.dev/tools/cli/serve#proxy)
