# Gun Slop – Catalog, Search + Shopping Cart (weeks 1–3)

ASP.NET Core 8 Web API + SQLite (EF Core) + plain HTML/CSS/JS frontend.

## Run
    dotnet restore
    dotnet run
Open the URL printed in the console (e.g. http://localhost:5000). Swagger: /swagger (Development).
The SQLite file `gunslop.db` is created and seeded automatically on first run.
(If you change the model later, delete gunslop.db to recreate it.)

## API
| Endpoint | Purpose |
|---|---|
| GET /api/products | list + search + filter + sort + paging |
| GET /api/products/{id} | product detail |
| GET /api/products/filters | categories, brands, calibers for dropdowns |

Query params: q, categoryId (a parent also matches its sub-categories), brand, caliber, minPrice, maxPrice, inStockOnly, sort (name|price_asc|price_desc|newest), page, pageSize

## Database (SQLite, created by EF Core from AppDbContext)
Tables: Users, Categories (tree), Products, CartItems, Orders, OrderItems, Payments, Reviews.
Models are in `Models/`, mapping and constraints in `Data/AppDbContext.cs`, sample data in `Data/SeedData.cs`.
**After changing a model, delete `gunslop.db` so it is recreated.**

## Cart API
The cart belongs to a user (CartItems.UserId). Until accounts exist (week 4) the cart uses the seeded demo user
`customer@gunslop.test` (see `CartController.CurrentUserId()`).
| Endpoint | Purpose |
|---|---|
| GET /api/cart | current cart |
| POST /api/cart/items `{productId, quantity}` | add (checks stock) |
| PUT /api/cart/items/{productId} `{quantity}` | change quantity (0 = remove) |
| DELETE /api/cart/items/{productId} | remove a line |
| DELETE /api/cart | empty the cart |
