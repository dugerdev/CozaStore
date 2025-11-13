# 🏗️ COZASTORE PROJESİ - KATMAN KATMAN MANTIK AÇIKLAMASI

## 📋 İÇİNDEKİLER
1. [Genel Mimari](#genel-mimari)
2. [Katman 1: Entities (Varlık Katmanı)](#katman-1-entities)
3. [Katman 2: Core (Çekirdek Katman)](#katman-2-core)
4. [Katman 3: DataAccess (Veri Erişim Katmanı)](#katman-3-dataaccess)
5. [Katman 4: Business (İş Mantığı Katmanı)](#katman-4-business)
6. [Katman 5: WebAPI (Sunum Katmanı - API)](#katman-5-webapi)
7. [Veri Akışı Örneği](#veri-akışı-örneği)

---

## 🎯 GENEL MİMARİ

### N-Tier (N-Katmanlı) Mimari Nedir?
Projeyi **5 ana katmana** ayırdık. Her katmanın **sadece kendi sorumluluğu** var:

```
┌─────────────────────────────────────┐
│   WebAPI (Sunum Katmanı)           │  ← Kullanıcıdan istek alır
├─────────────────────────────────────┤
│   Business (İş Mantığı)            │  ← İş kuralları, validasyon
├─────────────────────────────────────┤
│   DataAccess (Veri Erişim)         │  ← Veritabanı işlemleri
├─────────────────────────────────────┤
│   Core (Çekirdek/Abstractions)      │  ← Interface'ler, DTO'lar
├─────────────────────────────────────┤
│   Entities (Varlıklar)              │  ← Veritabanı tabloları
└─────────────────────────────────────┘
```

### Bağımlılık Yönü (Dependency Direction)
**ÖNEMLİ KURAL:** Üst katmanlar alt katmanlara bağımlıdır, ama alt katmanlar üst katmanları BİLMEZ!

- ✅ WebAPI → Business → DataAccess → Core → Entities
- ❌ Entities → Core → DataAccess → Business → WebAPI (YANLIŞ!)

---

## 📦 KATMAN 1: ENTITIES (Varlık Katmanı)

### Ne İşe Yarar?
**Veritabanı tablolarını** temsil eden sınıflar. En alt katman, hiçbir katmana bağımlı değil.

### Yapısı:

#### 1.1. BaseEntity.cs
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }                    // Her kayıt için benzersiz ID
    public DateTime CreatedDate { get; set; }        // Ne zaman oluşturuldu?
    public DateTime? UpdatedDate { get; set; }      // Ne zaman güncellendi?
    public bool IsActive { get; set; }              // Aktif mi?
    public bool IsDeleted { get; set; }             // Silinmiş mi? (Soft Delete)
    public DateTime? DeletedDate { get; set; }      // Ne zaman silindi?
}
```

**Neden Var?**
- Tüm tablolarda ortak alanlar (Id, CreatedDate vb.) var
- Her entity'de tekrar yazmak yerine **BaseEntity'den türetiyoruz**
- **Soft Delete:** Kayıtları fiziksel olarak silmek yerine `IsDeleted = true` yapıyoruz

#### 1.2. Product.cs (Örnek Entity)
```csharp
public class Product : BaseEntity  // BaseEntity'den türer
{
    public string Name { get; set; }           // Ürün adı
    public decimal Price { get; set; }        // Fiyat
    public Guid CategoryId { get; set; }       // Foreign Key (Hangi kategoriye ait?)
    
    // Navigation Properties (İlişkiler)
    public Category Category { get; set; }     // Bu ürünün kategorisi
    public ICollection<OrderDetail> OrderDetails { get; set; }  // Bu ürünün sipariş detayları
}
```

**Navigation Property Nedir?**
- EF Core'un ilişkileri yönetmesi için kullanılır
- `Category` → Bu ürünün kategorisini getirir
- `OrderDetails` → Bu ürünün sipariş detaylarını getirir

---

## 🔧 KATMAN 2: CORE (Çekirdek Katman)

### Ne İşe Yarar?
**Interface'ler (sözleşmeler)** ve **DTO'lar** (Data Transfer Objects) burada. Hiçbir implementasyon yok, sadece **tanımlar**.

### Yapısı:

#### 2.1. IRepository<T> (Generic Repository Interface)
```csharp
public interface IRepository<T> where T : BaseEntity
{
    // Sorgular (Query)
    Task<T?> GetByIdAsync(Guid id);                    // ID'ye göre getir
    Task<IEnumerable<T>> GetAllAsync();                 // Hepsini getir
    Task<IEnumerable<T>> FindAsync(Expression<...>);  // Koşula göre bul
    
    // Komutlar (Command)
    Task<T> AddAsync(T entity);                        // Ekle
    Task UpdateAsync(T entity);                        // Güncelle
    Task DeleteAsync(T entity);                        // Sil (fiziksel)
    Task SoftDeleteAsync(Guid id);                     // Soft delete
    
    // Kontrol
    Task<bool> ExistsAsync(Guid id);                   // Var mı?
}
```

**Neden Generic?**
- Her entity için ayrı repository yazmak yerine **tek bir interface** kullanıyoruz
- `IRepository<Product>`, `IRepository<Category>` → Hepsi aynı interface'i kullanır

#### 2.2. IUnitOfWork (Unit of Work Interface)
```csharp
public interface IUnitOfWork
{
    IRepository<Product> Products { get; }      // Product repository'si
    IRepository<Category> Categories { get; }    // Category repository'si
    IRepository<Order> Orders { get; }          // Order repository'si
    // ... diğer repository'ler
    
    Task<int> SaveChangesAsync();               // Tüm değişiklikleri kaydet
    Task BeginTransactionAsync();               // Transaction başlat
    Task CommitTransactionAsync();              // Transaction onayla
    Task RollbackTransactionAsync();           // Transaction geri al
}
```

**Neden Unit of Work?**
- Birden fazla repository'yi **tek bir yerden** yönetir
- **Transaction yönetimi:** Birden fazla işlem birlikte başarılı/başarısız olur
- **SaveChanges:** Tüm değişiklikler **tek seferde** veritabanına yazılır

**Örnek Senaryo:**
```csharp
// Sipariş oluştururken hem Order hem OrderDetail kaydetmemiz gerekiyor
await _unitOfWork.Orders.AddAsync(order);
await _unitOfWork.OrderDetails.AddRangeAsync(orderDetails);
await _unitOfWork.SaveChangesAsync();  // İKİSİ BİRLİKTE kaydedilir
```

#### 2.3. Result Pattern (Sonuç Deseni)
```csharp
// Başarılı sonuç
var result = new SuccessResult("Ürün eklendi.");

// Başarısız sonuç
var result = new ErrorResult("Ürün bulunamadı.");

// Veri ile sonuç
var result = new SuccessDataResult<Product>(product, "Ürün bulundu.");
```

**Neden Result Pattern?**
- İş katmanından dönen sonuçları **standartlaştırır**
- Her metod `Result` veya `DataResult<T>` döner
- Controller'da `if (result.Success)` ile kontrol ederiz

#### 2.4. DTOs (Data Transfer Objects)
```csharp
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    // ... sadece gerekli alanlar
}
```

**Neden DTO?**
- Entity'leri direkt API'ye döndürmek yerine **DTO kullanırız**
- Güvenlik: Gereksiz alanları gizleriz
- Esneklik: API'ye özel alanlar ekleyebiliriz

---

## 💾 KATMAN 3: DATAACCESS (Veri Erişim Katmanı)

### Ne İşe Yarar?
**Entity Framework Core** ile veritabanı işlemlerini yapar. Core'daki **interface'leri implement eder**.

### Yapısı:

#### 3.1. CozaStoreDbContext.cs
```csharp
public class CozaStoreDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, ...>
{
    public DbSet<Product> Products { get; set; }      // Products tablosu
    public DbSet<Category> Categories { get; set; }  // Categories tablosu
    // ... diğer tablolar
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);  // Identity tablolarını yapılandır
        
        // Fluent API konfigürasyonlarını uygula
        modelBuilder.ApplyConfigurationsFromAssembly(...);
    }
}
```

**Ne İşe Yarar?**
- EF Core'un **veritabanı bağlantısını** yönetir
- Her `DbSet<T>` bir **tabloyu** temsil eder
- `OnModelCreating`: Tablo yapılandırmalarını (Fluent API) uygular

#### 3.2. EfRepositoryBase<T> (Repository Implementasyonu)
```csharp
public class EfRepositoryBase<T> : IRepository<T> where T : BaseEntity
{
    private readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;
    
    public EfRepositoryBase(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();  // EF Core entity tipine göre DbSet'i verir
    }
    
    public async Task<T?> GetByIdAsync(Guid id)
    {
        // Soft delete edilmemiş kayıtları getir
        return await _dbSet.FirstOrDefaultAsync(
            entity => entity.Id == id && !entity.IsDeleted);
    }
    
    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);  // Sadece memory'ye ekler
        return entity;                  // SaveChanges çağrılana kadar DB'ye yazılmaz!
    }
}
```

**Önemli Noktalar:**
- `AddAsync` sadece **memory'ye ekler**, veritabanına yazmaz!
- `SaveChangesAsync()` çağrılana kadar değişiklikler **kalıcı olmaz**
- Soft delete kontrolü **otomatik** yapılır

#### 3.3. UnitOfWork.cs (Unit of Work Implementasyonu)
```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly CozaStoreDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();
    
    // Her entity için repository property'si
    public IRepository<Product> Products => GetRepository<Product>();
    public IRepository<Category> Categories => GetRepository<Category>();
    // ...
    
    private IRepository<TEntity> GetRepository<TEntity>()
    {
        var type = typeof(TEntity);
        
        // Daha önce oluşturulmuş mu kontrol et (cache)
        if (!_repositories.TryGetValue(type, out var repository))
        {
            repository = new EfRepositoryBase<TEntity>(_context);  // İlk defa oluştur
            _repositories[type] = repository;                      // Cache'e ekle
        }
        
        return (IRepository<TEntity>)repository;
    }
    
    public async Task<int> SaveChangesAsync()
    {
        // Tüm repository'lerdeki değişiklikleri tek seferde kaydet
        return await _context.SaveChangesAsync();
    }
}
```

**Nasıl Çalışır?**
1. `UnitOfWork.Products.AddAsync(...)` → `EfRepositoryBase<Product>` oluşturulur
2. `UnitOfWork.Categories.AddAsync(...)` → `EfRepositoryBase<Category>` oluşturulur
3. `UnitOfWork.SaveChangesAsync()` → **İKİSİ BİRLİKTE** veritabanına yazılır

**Neden Cache?**
- Aynı entity için repository **tekrar oluşturulmaz**
- Performans artışı

---

## 🧠 KATMAN 4: BUSINESS (İş Mantığı Katmanı)

### Ne İşe Yarar?
**İş kurallarını** (business rules) uygular. Validasyon, kontrol, mantık burada.

### Yapısı:

#### 4.1. IProductService.cs (Service Interface)
```csharp
public interface IProductService
{
    Task<DataResult<Product>> GetByIdAsync(Guid id);
    Task<DataResult<IEnumerable<Product>>> GetAllAsync();
    Task<Result> AddAsync(Product product);
    Task<Result> UpdateAsync(Product product);
    Task<Result> DeleteAsync(Guid id);
}
```

**Neden Interface?**
- **Dependency Inversion Principle (SOLID)**
- Controller `IProductService` kullanır, `ProductManager`'ı bilmez
- Test edilebilirlik artar (Mock kullanabiliriz)

#### 4.2. ProductManager.cs (Service Implementasyonu)
```csharp
public class ProductManager : IProductService
{
    private readonly IUnitOfWork _unitOfWork;        // Repository'lere erişim
    private readonly IValidator<Product> _validator; // FluentValidation
    
    public ProductManager(IUnitOfWork unitOfWork, IValidator<Product> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }
    
    public async Task<Result> AddAsync(Product product)
    {
        // 1. Validasyon kontrolü
        await _validator.ValidateAndThrowAsync(product);
        
        // 2. İş kuralı: Stok kontrolü
        if (product.StockQuantity < 0)
            return new ErrorResult("Stok miktarı negatif olamaz.");
        
        // 3. Repository'ye ekle
        await _unitOfWork.Products.AddAsync(product);
        
        // 4. Değişiklikleri kaydet
        await _unitOfWork.SaveChangesAsync();
        
        return new SuccessResult("Ürün eklendi.");
    }
}
```

**İş Mantığı Örnekleri:**
- ✅ Validasyon (FluentValidation)
- ✅ İş kuralları (stok kontrolü, fiyat kontrolü vb.)
- ✅ Soft delete uygulama
- ✅ Transaction yönetimi

#### 4.3. ProductValidator.cs (FluentValidation)
```csharp
public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Ürün adı boş olamaz.")
            .MaximumLength(200).WithMessage("Ürün adı en fazla 200 karakter olabilir.");
        
        RuleFor(p => p.Price)
            .GreaterThan(0).WithMessage("Fiyat 0'dan büyük olmalıdır.");
    }
}
```

---

## 🌐 KATMAN 5: WEBAPI (Sunum Katmanı - API)

### Ne İşe Yarar?
**HTTP isteklerini** alır, Business katmanını çağırır, **JSON** döner.

### Yapısı:

#### 5.1. Program.cs (Uygulama Yapılandırması)
```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. DbContext'i kaydet
builder.Services.AddDbContext<CozaStoreDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Identity'yi kaydet
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(...)
    .AddEntityFrameworkStores<CozaStoreDbContext>();

// 3. UnitOfWork'ü kaydet (Scoped: Her HTTP isteği için yeni instance)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 4. Business servislerini kaydet
builder.Services.AddCozaStoreBusiness();

// 5. JWT Authentication'ı yapılandır
builder.Services.AddAuthentication(...)
    .AddJwtBearer(...);

var app = builder.Build();

// Middleware sırası ÖNEMLİ!
app.UseCors("AllowWebUI");      // 1. CORS
app.UseAuthentication();         // 2. Kimlik doğrulama
app.UseAuthorization();          // 3. Yetkilendirme
app.MapControllers();            // 4. Controller'ları map et

app.Run();
```

**Dependency Injection (DI) Nedir?**
- `AddScoped<IUnitOfWork, UnitOfWork>` → Her HTTP isteği için **yeni bir UnitOfWork** oluşturulur
- Controller'a `IProductService` enjekte edilir, `ProductManager` otomatik oluşturulur

#### 5.2. ProductsController.cs (API Controller)
```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;  // Business katmanı
    
    public ProductsController(IProductService productService)
    {
        _productService = productService;  // DI ile enjekte edilir
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // 1. Business katmanını çağır
        var result = await _productService.GetAllAsync();
        
        // 2. Sonucu kontrol et
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        
        // 3. Entity'yi DTO'ya çevir
        var products = result.Data.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            // ...
        }).ToList();
        
        // 4. JSON olarak döndür
        return Ok(products);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]  // Sadece Admin yapabilir
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        var result = await _productService.AddAsync(product);
        
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
}
```

**Controller'ın Sorumluluğu:**
- ✅ HTTP isteklerini almak
- ✅ Business katmanını çağırmak
- ✅ Entity → DTO dönüşümü
- ✅ HTTP status kodları döndürmek (200, 400, 404 vb.)
- ❌ İş mantığı yazmak (Business katmanında olmalı!)
- ❌ Veritabanı işlemleri yapmak (DataAccess katmanında olmalı!)

---

## 🔄 VERİ AKIŞI ÖRNEĞİ

### Senaryo: Yeni Ürün Ekleme (POST /api/products)

```
1. Kullanıcı → HTTP POST isteği gönderir
   POST /api/products
   Body: { "name": "Laptop", "price": 15000, ... }

2. ProductsController.Create() → İstek alınır
   - [FromBody] ile JSON'dan Product nesnesi oluşturulur
   - _productService.AddAsync(product) çağrılır

3. ProductManager.AddAsync() → İş mantığı
   - FluentValidation ile validasyon yapılır
   - İş kuralları kontrol edilir (stok, fiyat vb.)
   - _unitOfWork.Products.AddAsync(product) çağrılır

4. UnitOfWork.Products → Repository'ye erişim
   - GetRepository<Product>() → EfRepositoryBase<Product> döner
   - EfRepositoryBase.AddAsync(product) çağrılır

5. EfRepositoryBase.AddAsync() → EF Core işlemi
   - _dbSet.AddAsync(product) → Memory'ye eklenir
   - Henüz veritabanına yazılmadı!

6. ProductManager → SaveChanges
   - _unitOfWork.SaveChangesAsync() çağrılır

7. UnitOfWork.SaveChangesAsync() → EF Core SaveChanges
   - _context.SaveChangesAsync() → ŞİMDİ veritabanına yazılır!

8. ProductManager → Sonuç döner
   - return new SuccessResult("Ürün eklendi.")

9. ProductsController → HTTP Response
   - 201 Created döner
   - Location header'ı eklenir

10. Kullanıcı → Başarılı yanıt alır
```

### Önemli Noktalar:

1. **AddAsync() → SaveChanges() Ayrımı:**
   - `AddAsync()` sadece memory'ye ekler
   - `SaveChangesAsync()` veritabanına yazar
   - Bu sayede **transaction** yönetimi yapabiliriz

2. **Soft Delete:**
   - `DeleteAsync()` → `IsDeleted = true` yapar
   - `GetAllAsync()` → `IsDeleted = false` olanları getirir
   - Veritabanından **fiziksel olarak silinmez**

3. **Dependency Injection:**
   - Her HTTP isteği için **yeni bir UnitOfWork** oluşturulur
   - İstek bitince otomatik dispose edilir

---

## 🎓 ÖZET

### Katman Sorumlulukları:

| Katman | Sorumluluk | Örnek |
|--------|-----------|-------|
| **Entities** | Veritabanı tabloları | Product, Category |
| **Core** | Interface'ler, DTO'lar | IRepository, ProductDto |
| **DataAccess** | EF Core işlemleri | EfRepositoryBase, UnitOfWork |
| **Business** | İş mantığı, validasyon | ProductManager, ProductValidator |
| **WebAPI** | HTTP istekleri, JSON | ProductsController |

### Bağımlılık Kuralı:
```
WebAPI → Business → DataAccess → Core → Entities
```

### Temel Prensipler:
1. **Separation of Concerns:** Her katman kendi işini yapar
2. **Dependency Inversion:** Interface'ler üzerinden çalışırız
3. **Single Responsibility:** Her sınıf tek bir sorumluluğa sahip
4. **DRY (Don't Repeat Yourself):** Generic repository, base entity

---

## ❓ SIK SORULAN SORULAR

### 1. Neden Generic Repository?
- Her entity için ayrı repository yazmak yerine tek bir sınıf kullanırız
- Kod tekrarını önler

### 2. Neden Unit of Work?
- Birden fazla repository'yi tek yerden yönetir
- Transaction yönetimi yapar
- SaveChanges tek seferde çağrılır

### 3. Neden Result Pattern?
- İş katmanından dönen sonuçları standartlaştırır
- Controller'da `if (result.Success)` ile kontrol ederiz

### 4. Neden DTO?
- Entity'leri direkt API'ye döndürmek güvenli değil
- Gereksiz alanları gizleriz
- API'ye özel alanlar ekleyebiliriz

### 5. Neden Soft Delete?
- Veriler fiziksel olarak silinmez
- Geri getirme imkanı vardır
- Audit (denetim) için önemlidir

---

## 🚀 SONUÇ

Bu mimari sayesinde:
- ✅ Kod tekrarı azalır
- ✅ Test edilebilirlik artar
- ✅ Bakım kolaylaşır
- ✅ Ölçeklenebilirlik sağlanır
- ✅ SOLID prensipleri uygulanır

Her katman **bağımsız** geliştirilebilir ve **değiştirilebilir**!

