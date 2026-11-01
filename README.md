# Gun Slop – Catalog + Basic Search (weeks 1–2)

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

Query params: q, category (Firearm/Ammunition/Accessory), brand, caliber, minPrice, maxPrice, inStockOnly, sort (name|price_asc|price_desc|newest), page, pageSize
