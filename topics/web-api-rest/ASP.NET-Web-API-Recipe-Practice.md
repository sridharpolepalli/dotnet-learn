# Step-by-Step Practice: Recipe Web API (Onion Architecture)

**Hands-on guide** for building a **Recipe Web API** in ASP.NET Core using **Onion architecture**: Repository layer (EF Core, Repository pattern), Contract layer (DTOs), Application layer (services), and API layer (controllers). Includes **Swagger** setup, **testing** (Swagger UI, Postman, other tools), and a **.NET Console client** sample.

---

## Table of Contents

| Part | Topic |
|------|--------|
| 1 | [Overview](#overview) |
| 2 | [Repository layer](#building-the-repository-layer) — Database, entities, DbContext, repositories |
| 3 | [Contract layer (DTOs)](#building-contract-layer-dtos) |
| 4 | [Application layer](#building-the-application-layer) — Services |
| 5 | [API layer](#building-the-controllers) — Controllers |
| 6 | [Enable Swagger](#step-11-enable-swagger) |
| 7 | [Testing](#testing-the-api) — Swagger UI, Postman, other tools |
| 8 | [Sample .NET Console client](#sample-net-console-client) |
| — | [Step-by-step Angular client](../fullstack-clients/Recipe-API-Angular-Client.md) — Setup, implement, run (detailed) |

---

## Overview

This practice builds a **Recipe Web API** with:

- **Onion architecture:** API (controllers) → Application (services) → Domain/Contract (DTOs, entities) → Infrastructure (repositories, DbContext).
- **Repository pattern:** Abstracts data access behind interfaces (`IRecipeRepository`, `ICategoryRepository`, `IIngredientRepository`).
- **EF Core** for data access (SQL Server). You can add **Dapper** for specific queries if needed.
- **CRUD** for Categories (and similarly for Recipes, Ingredients): GET all, GET by id, POST, PUT, DELETE.

---

## Building the Repository Layer

### Step 1: Create database objects

Run the following SQL on your SQL Server instance (e.g. SSMS or Azure Data Studio) to create the **Recipe** database and tables.

```sql
CREATE TABLE Category (
    Id INT PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    CreatedBy NVARCHAR(100) NOT NULL,
    CreatedOn DATETIME NOT NULL,
    UpdatedBy NVARCHAR(100) NOT NULL,
    UpdatedOn DATETIME NOT NULL
);

CREATE TABLE Ingredient (
    Id INT PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    CreatedBy NVARCHAR(100) NOT NULL,
    CreatedOn DATETIME NOT NULL,
    UpdatedBy NVARCHAR(100) NOT NULL,
    UpdatedOn DATETIME NOT NULL
);

CREATE TABLE Recipe (
    Id INT PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    CategoryId INT,
    IngredientJson NVARCHAR(MAX),
    Instructions NVARCHAR(MAX),
    CreatedBy NVARCHAR(100) NOT NULL,
    CreatedOn DATETIME NOT NULL,
    UpdatedBy NVARCHAR(100) NOT NULL,
    UpdatedOn DATETIME NOT NULL,
    CONSTRAINT FK_Recipes_Category FOREIGN KEY (CategoryId)
        REFERENCES Category(Id)
        ON DELETE SET NULL
);
```

---

### Step 2: Define entity classes

Create a **BaseEntity** and entity classes mapped to the tables. Use **`[Table("Category")]`** (no trailing space) so EF Core maps to the correct table name.

```csharp
using System.ComponentModel.DataAnnotations.Schema;

public class BaseEntity
{
    public string CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime UpdatedOn { get; set; }
}

[Table("Category")]
public class Category : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Recipe> Recipes { get; set; }
}

[Table("Ingredient")]
public class Ingredient : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[Table("Recipe")]
public class Recipe : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
    public string IngredientJson { get; set; }
    public string Instructions { get; set; }
}
```

---

### Step 3: Configure DbContext

**Install EF Core packages** (in the API project):

```powershell
Install-Package Microsoft.EntityFrameworkCore
Install-Package Microsoft.EntityFrameworkCore.SqlServer
```

**AppDbContext:**

```csharp
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>()
            .HasOne(r => r.Category)
            .WithMany(c => c.Recipes)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            entry.Entity.CreatedBy ??= "system";
            entry.Entity.UpdatedBy ??= "system";
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOn = DateTime.UtcNow;
                    entry.Entity.UpdatedOn = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedOn = DateTime.UtcNow;
                    break;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
```

---

### Step 4: Connection string (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=Recipe;User ID=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  },
  "AllowedHosts": "*"
}
```

Replace `YOUR_SERVER`, `YOUR_USER`, and `YOUR_PASSWORD` with your SQL Server details (e.g. `localhost\\SQLEXPRESS` and Windows auth or SQL auth).

---

### Step 5: Register DbContext in DI (Program.cs)

Ensure the **API project** has the EF Core SqlServer package, then in **Program.cs**:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

- **AddDbContext&lt;AppDbContext&gt;** — Registers the custom DbContext in the DI container (scoped by default).
- **UseSqlServer(...)** — Configures EF Core to use SQL Server with the given connection string.

---

### Step 6: Repository implementation

**Interfaces:**

```csharp
public interface IRecipeRepository
{
    Task<List<Recipe>> GetAllRecipesAsync();
    Task<Recipe> GetRecipeByIdAsync(int id);
    Task AddRecipeAsync(Recipe recipe);
    Task UpdateRecipeAsync(Recipe recipe);
    Task DeleteRecipeAsync(int id);
}

public interface ICategoryRepository
{
    Task<List<Category>> GetAllCategoriesAsync();
    Task<Category> GetCategoryByIdAsync(int id);
    Task AddCategoryAsync(Category category);
    Task UpdateCategoryAsync(Category category);
    Task DeleteCategoryAsync(int id);
}

public interface IIngredientRepository
{
    Task<List<Ingredient>> GetAllIngredientsAsync();
    Task<Ingredient> GetIngredientByIdAsync(int id);
    Task AddIngredientAsync(Ingredient ingredient);
    Task UpdateIngredientAsync(Ingredient ingredient);
    Task DeleteIngredientAsync(int id);
}
```

**RecipeRepository:**

```csharp
public class RecipeRepository : IRecipeRepository
{
    private readonly AppDbContext _context;
    public RecipeRepository(AppDbContext context) => _context = context;

    public async Task<List<Recipe>> GetAllRecipesAsync() => await _context.Recipes.ToListAsync();
    public async Task<Recipe> GetRecipeByIdAsync(int id) => await _context.Recipes.FindAsync(id);

    public async Task AddRecipeAsync(Recipe recipe)
    {
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRecipeAsync(Recipe recipe)
    {
        _context.Entry(recipe).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRecipeAsync(int id)
    {
        var recipe = await _context.Recipes.FindAsync(id);
        if (recipe != null)
        {
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
        }
    }
}
```

**CategoryRepository** and **IngredientRepository** follow the same pattern (replace `Recipes` with `Categories` or `Ingredients` and the corresponding entity type).

---

### Step 7: Register repositories in DI (Program.cs)

```csharp
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();
```

---

## Building Contract Layer (DTOs)

A **DTO (Data Transfer Object)** is a simple object used to transfer data between layers without exposing domain internals or behavior.

```csharp
public class CategoryDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

Define similar DTOs for Recipe and Ingredient as needed (e.g. RecipeDTO with Id, Name, CategoryId, IngredientJson, Instructions).

---

## Building the Application Layer

### Step 8: Implement application layer (CategoryService)

**ICategoryService:**

```csharp
public interface ICategoryService
{
    Task<List<CategoryDTO>> GetAllCategoriesAsync();
    Task<CategoryDTO> GetCategoryByIdAsync(int id);
    Task AddCategoryAsync(CategoryDTO category);
    Task UpdateCategoryAsync(CategoryDTO category);
    Task DeleteCategoryAsync(int id);
}
```

**CategoryService:**

```csharp
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    public CategoryService(ICategoryRepository categoryRepository) => _categoryRepository = categoryRepository;

    public async Task<List<CategoryDTO>> GetAllCategoriesAsync()
    {
        var categories = await _categoryRepository.GetAllCategoriesAsync();
        return categories.Select(c => new CategoryDTO { Id = c.Id, Name = c.Name }).ToList();
    }

    public async Task<CategoryDTO> GetCategoryByIdAsync(int id)
    {
        var category = await _categoryRepository.GetCategoryByIdAsync(id);
        return category == null ? null : new CategoryDTO { Id = category.Id, Name = category.Name };
    }

    public async Task AddCategoryAsync(CategoryDTO dto)
    {
        await _categoryRepository.AddCategoryAsync(new Category { Id = dto.Id, Name = dto.Name });
    }

    public async Task UpdateCategoryAsync(CategoryDTO dto)
    {
        await _categoryRepository.UpdateCategoryAsync(new Category { Id = dto.Id, Name = dto.Name });
    }

    public async Task DeleteCategoryAsync(int id) => await _categoryRepository.DeleteCategoryAsync(id);
}
```

---

### Step 9: Register services in DI (Program.cs)

```csharp
builder.Services.AddScoped<ICategoryService, CategoryService>();
```

(Repositories are already registered in Step 7.)

---

## Building the Controllers

### Step 10: CategoryController

```csharp
[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    public CategoryController(ICategoryService categoryService) => _categoryService = categoryService;

    [HttpGet]
    public async Task<ActionResult<List<CategoryDTO>>> GetAllCategories()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDTO>> GetCategoryById(int id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null) return NotFound();
        return Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory([FromBody] CategoryDTO category)
    {
        await _categoryService.AddCategoryAsync(category);
        return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDTO category)
    {
        if (id != category.Id) return BadRequest();
        await _categoryService.UpdateCategoryAsync(category);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await _categoryService.DeleteCategoryAsync(id);
        return NoContent();
    }
}
```

---

## Step 11: Enable Swagger

Follow these steps so **Swagger UI** is available when you run the API in Visual Studio (or from the command line).

### 11.1 Install NuGet packages

In the **API project**, install:

```powershell
Install-Package Swashbuckle.AspNetCore
```

Or via .NET CLI:

```bash
dotnet add package Swashbuckle.AspNetCore
```

### 11.2 Register Swagger in Program.cs

Add the following **before** `var app = builder.Build();`:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Recipe API", Version = "v1" });
});
```

Add at the top of the file if needed:

```csharp
using Microsoft.OpenApi.Models;
```

Add **after** `var app = builder.Build();` (and before `app.MapControllers()`), so Swagger is available in **Development**:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Recipe API v1");
    });
}
```

### 11.3 (Optional) Launch Swagger when running from Visual Studio

In **Properties/launchSettings.json**, under your launch profile (e.g. the one with `"commandName": "Project"`), add or ensure:

```json
"launchBrowser": true,
"launchUrl": "swagger"
```

Example profile:

```json
"RecipeApi": {
  "commandName": "Project",
  "launchBrowser": true,
  "launchUrl": "swagger",
  "applicationUrl": "https://localhost:7071;http://localhost:5071",
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Development"
  }
}
```

When you press **F5** or click **Run**, the browser will open to **https://localhost:7071/swagger** (or your configured URL + `/swagger`).

### 11.4 Summary

| Step | Action |
|------|--------|
| 1 | Install **Swashbuckle.AspNetCore**. |
| 2 | Call **AddEndpointsApiExplorer()** and **AddSwaggerGen()** with a document name (e.g. "v1"). |
| 3 | In Development, call **UseSwagger()** and **UseSwaggerUI()** with the same document. |
| 4 | Set **launchUrl** to **swagger** in launchSettings.json so the browser opens Swagger when you run the app. |

---

## Testing the API

### Testing from Swagger UI (sample screen)

When you open **https://localhost:7071/swagger** (or your base URL + `/swagger`), you see:

1. **Title** at the top (e.g. "Recipe API v1").
2. **List of endpoints** grouped by controller (e.g. **Categories**):
   - **GET /api/categories** — Get all categories
   - **GET /api/categories/{id}** — Get category by id
   - **POST /api/categories** — Add category
   - **PUT /api/categories/{id}** — Update category
   - **DELETE /api/categories/{id}** — Delete category
3. Each endpoint can be **expanded**. Click **Try it out**, fill in parameters (e.g. **id** for GET by id) or **Request body** (for POST/PUT), then click **Execute**.
4. The **Response** section shows the **status code** (e.g. 200, 201, 404) and **response body** (e.g. JSON). You can copy the response or the **CURL** command from the screen.

**Example flow:** Expand **POST /api/categories** → **Try it out** → Edit body to `{ "id": 1, "name": "Appetizer" }` → **Execute** → See **201 Created** and response body; then use **GET /api/categories/1** to verify.

### Testing from Postman

1. **Create a request:** New → HTTP Request.
2. **Set method and URL:** e.g. **GET** `https://localhost:7071/api/categories`.
3. **Send:** Click **Send**. View status code and body (e.g. JSON).
4. **POST/PUT:** Choose **POST** or **PUT**, go to **Body** → **raw** → **JSON**, and enter payload, e.g. `{ "id": 1, "name": "Appetizer" }`.
5. **Save** requests in a **Collection** (e.g. "Recipe API") for reuse.

If you use a self-signed HTTPS certificate, you may need to turn off **SSL certificate verification** in Postman settings (for local dev only).

### Other tools for testing REST APIs

| Tool | Description |
|------|-------------|
| **Postman** | GUI; collections, environment variables, scripts. Very common. |
| **Swagger UI** | Built into the API; try endpoints from the browser. |
| **curl** | Command line. Example: `curl -X GET https://localhost:7071/api/categories -k` |
| **HTTPie** | Command-line, human-friendly. Example: `http GET https://localhost:7071/api/categories` |
| **REST Client (VS Code)** | Extension; send HTTP requests from `.http` files in the editor. |
| **Insomnia** | Similar to Postman; lightweight. |
| **Bruno** | Open-source, file-based API client. |
| **Thunder Client (VS Code)** | Lightweight REST client inside VS Code. |
| **Browser** | For GET only; open the URL (e.g. `https://localhost:7071/api/categories`) to see JSON. |

---

## Sample .NET Console Client

A minimal **.NET Console** app that calls the Recipe API (Categories) using **HttpClient**.

### 1. Create the console project

```bash
dotnet new console -n RecipeApiClient -o RecipeApiClient
cd RecipeApiClient
```

### 2. Add package for JSON (if not using .NET 6+ in-built support)

For **System.Text.Json** (included in .NET 6+), no extra package is needed. Ensure your project targets .NET 6 or later.

### 3. Add a reference to the DTO (optional)

If the client is in the same solution, you can reference the API project to reuse **CategoryDTO**. Otherwise, define a simple class in the console app:

```csharp
public class CategoryDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

### 4. Example: GET all categories and GET by id

**Program.cs** (or a separate class):

```csharp
using System.Net.Http.Json;
using System.Text.Json;

var baseAddress = "https://localhost:7071/";  // Match your API URL
using var handler = new HttpClientHandler();
handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;  // Dev only: ignore SSL
using var client = new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

// GET all categories
var categories = await client.GetFromJsonAsync<List<CategoryDTO>>("api/categories");
Console.WriteLine($"Found {categories?.Count ?? 0} categories");
foreach (var c in categories ?? new List<CategoryDTO>())
    Console.WriteLine($"  Id: {c.Id}, Name: {c.Name}");

// GET by id
int id = 1;
var category = await client.GetFromJsonAsync<CategoryDTO>($"api/categories/{id}");
if (category != null)
    Console.WriteLine($"Category {id}: {category.Name}");
else
    Console.WriteLine($"Category {id} not found.");
```

### 5. Example: POST (create category)

```csharp
var newCategory = new CategoryDTO { Id = 10, Name = "Dessert" };
var response = await client.PostAsJsonAsync("api/categories", newCategory);
response.EnsureSuccessStatusCode();
Console.WriteLine($"Created: {response.Headers.Location}");
```

### 6. Run

- Start the **Recipe API** first (e.g. from Visual Studio or `dotnet run` in the API project).
- Run the console client: `dotnet run` from the **RecipeApiClient** folder.

You can extend the console app with **PUT** and **DELETE** using `client.PutAsJsonAsync` and `client.DeleteAsync`.

---

## Angular client (step-by-step)

For a **detailed step-by-step implementation** of an **Angular** client that consumes this Recipe API (Categories CRUD, services, components, routing, run, and CORS/proxy), see:

**[Step-by-Step: Angular Client for Recipe Web API](../fullstack-clients/Recipe-API-Angular-Client.md)**

That guide covers: prerequisites (Node, Angular CLI), creating the project, environment configuration, models, `CategoryService` (HttpClient), category list/detail/form components, routing, running with the API, and optional CORS or proxy setup.

---

## References

- [ASP.NET Core Web API](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Swashbuckle / Swagger in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger)
- [HttpClient and HttpClientFactory](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient)
