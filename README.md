# MB.GenericBulkInsert NuGet Package

## Overview
**MB.GenericBulkInsert** library allows you to perform fast and high-performance **Bulk Insert** operations on **SQL Server** using **Entity Framework Core (EF Core)**.

## Features
- ✅ **Fast and Efficient**: Uses `SqlBulkCopy` to quickly insert large datasets.
- ✅ **Full EF Core Compatibility**: Easily integrated into your EF Core `DbContext`.
- ✅ **Transaction Support**: Database operations are wrapped in a transaction for rollback in case of failure.
- ✅ **Easy to Use**: Perform bulk insert with just a few lines of code.

## Getting Started

## Installation
To integrate `MB.GenericBulkInsert` into your project, install it via the NuGet package manager:

```plaintext
Install-Package MB.GenericBulkInsert
```
Or through the .NET CLI:
```plaintext
dotnet add package MB.GenericBulkInsert
```

## **🛠 Usage**

### **1️ Define Your EF Core Model**

First, define your data model:

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```
### **2️ Define Your DbContext**

Create a `DbContext`:

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=.;Database=BulkInsertDB;Trusted_Connection=True;");
    }
}
```
### **3️ Use Bulk Insert**

You can now use the Bulk Insert method as shown below:

```csharp
using var context = new AppDbContext();

// 📌 Create 100,000 products
var products = Enumerable.Range(1, 100_000)
    .Select(i => new Product { Name = $"Product {i}", Price = i * 1.5m })
    .ToList();

// 📌 Call Bulk Insert
await context.BulkInsertAsync(products);

Console.WriteLine("Bulk insert completed!");

```
## **⚡ Performance**

| **Method**                            | **10,000 Records (ms)** | **100,000 Records (ms)**   |
|---------------------------------------|-------------------------|----------------------------|
| `AddRangeAsync()` + `SaveChangesAsync()` | 8500ms                  | 85,000ms (85s)             |
| `BulkInsertAsync()` (This Library)    | 450ms                   | 4200ms (4.2s)              |

**BulkInsertAsync** is up to 20 times faster than EF Core's default method!


## **🔧 Configuration**

You can customize the **batch size** to optimize performance based on the size of the data being inserted:

```csharp
await context.BulkInsertAsync(products, batchSize: 10000); // Batch size of 10,000
```

## **📜 License**

The **MB.GenericBulkInsert** library is licensed under the **MIT License**.  

## **🌍 Links**

- **NuGet Page**: [NuGet](https://www.nuget.org/packages/MB.GenericBulkInsert)
- **GitHub Repo**: [GitHub](https://github.com/mslmbitgen/MB.GenericBulkInsert)

## **🙌 Contributing**

If you'd like to contribute to this project, feel free to open a **pull request**.  
For any questions or suggestions, please use the **Issues** section.
