using Microsoft.AspNetCore.Mvc;
using Beltelecom.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Beltelecom.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Beltelecom.Models.ViewModels;
using static NuGet.Packaging.PackagingConstants;

namespace Beltelecom.Controllers
{
    [Authorize(Roles = "Admin")] // Доступ только для администраторов
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Страница для просмотра всех товаров
        public IActionResult ProductList(string? search)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower(); // для нечувствительности к регистру
                products = products.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    p.Category.Name.ToLower().Contains(search) ||
                    p.Brand.Name.ToLower().Contains(search));
            }

            return View(products.ToList());
        }



        // Страница для добавления нового товара
        public IActionResult Create()
        {
            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name");
            ViewData["Brands"] = new SelectList(_context.Brands, "Id", "Name");
            return View(new Product());
        }

        // Обработка добавления нового товара
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(Product product)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        product.CreatedAt = DateTime.Now;

        //        // Сохраняем продукт (без характеристик)
        //        _context.Add(product);
        //        await _context.SaveChangesAsync(); // получаем product.Id

        //        // Обработка характеристик
        //        var specNames = Request.Form["SpecificationName"];
        //        var specValues = Request.Form["SpecificationValue"];
        //        var specGroups = Request.Form["SpecificationGroup"];

        //        var specifications = new List<ProductSpecification>();
        //        for (int i = 0; i < specNames.Count; i++)
        //        {
        //            if (!string.IsNullOrWhiteSpace(specNames[i]) && !string.IsNullOrWhiteSpace(specValues[i]))
        //            {
        //                specifications.Add(new ProductSpecification
        //                {
        //                    ProductId = product.Id,
        //                    Name = specNames[i],
        //                    Value = specValues[i],
        //                    Group = specGroups[i]
        //                });
        //            }
        //        }

        //        if (specifications.Any())
        //        {
        //            _context.ProductSpecifications.AddRange(specifications);
        //            await _context.SaveChangesAsync();
        //        }

        //        return RedirectToAction(nameof(ProductList));
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
        //        {
        //            Console.WriteLine(error.ErrorMessage);
        //        }

        //        ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        //        ViewData["Brands"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
        //        return View(product);
        //    }

        //    ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        //    ViewData["Brands"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
        //    return View(product);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                // Устанавливаем Category и Brand, если ID были переданы
                product.Category = await _context.Categories.FindAsync(product.CategoryId);
                product.Brand = await _context.Brands.FindAsync(product.BrandId);

                product.CreatedAt = DateTime.Now;

                var specs = product.Specifications?.Where(s =>
                    !string.IsNullOrWhiteSpace(s.Name) &&
                    !string.IsNullOrWhiteSpace(s.Value)).ToList();

                foreach (var prop in product.GetType().GetProperties())
                {
                    var value = prop.GetValue(product);
                    Console.WriteLine($"{prop.Name}: {value}");
                }
                product.Specifications = null;

                _context.Add(product);
                await _context.SaveChangesAsync();

                if (specs != null && specs.Any())
                {
                    foreach (var spec in specs)
                    {
                        spec.Id = 0;
                        spec.ProductId = product.Id;
                    }

                    _context.ProductSpecifications.AddRange(specs);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(ProductList));
            }

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                foreach (var prop in product.GetType().GetProperties())
                {
                    var value = prop.GetValue(product);
                    Console.WriteLine($"{prop.Name}: {value}");
                }

                ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
                ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
                return View(product);
            }
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            return View(product);
        }


        // Страница для редактирования товара
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Specifications)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            product.Specifications ??= new List<ProductSpecification>();

            var viewModel = new ProductEditViewModel
            {
                Product = product,
                Specifications = product.Specifications
                    .Select(s => new ProductSpecification
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Group = s.Group,
                        Value = s.Value
                    })
                    .ToList()
            };

            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["Brands"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);

            return View(viewModel);
        }



        // Обработка редактирования товара
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel model)
        {
            if (id != model.Product.Id)
            {
                return NotFound();
            }

            // Обязательно проверяем null перед обращением к спискам
            model.Specifications ??= new List<ProductSpecification>();

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                foreach (var prop in model.Product.GetType().GetProperties())
                {
                    var value = prop.GetValue(model.Product);
                    Console.WriteLine($"{prop.Name}: {value}");
                }

                ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name", model.Product.CategoryId);
                ViewData["Brands"] = new SelectList(_context.Brands, "Id", "Name", model.Product.BrandId);
                return View(model);
            }

            var existingProduct = await _context.Products
                .Include(p => p.Specifications)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            // Обновление данных
            existingProduct.Name = model.Product.Name;
            existingProduct.Description = model.Product.Description;
            existingProduct.Price = model.Product.Price;
            existingProduct.CategoryId = model.Product.CategoryId;
            existingProduct.BrandId = model.Product.BrandId;
            existingProduct.ImageUrl = model.Product.ImageUrl;
            existingProduct.Stock = model.Product.Stock;
            existingProduct.WarrantyMonths = model.Product.WarrantyMonths;
            existingProduct.InstallmentMonths = model.Product.InstallmentMonths;
            existingProduct.CreatedAt = model.Product.CreatedAt;

            // Обновление характеристик
            foreach (var spec in model.Specifications)
            {
                var existingSpec = existingProduct.Specifications.FirstOrDefault(s => s.Id == spec.Id);
                if (existingSpec != null)
                {
                    existingSpec.Value = spec.Value;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ProductList));
        }

        // Обработка удаления товара
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product != null)
            {
                var specs = _context.ProductSpecifications.Where(s => s.ProductId == id);
                _context.ProductSpecifications.RemoveRange(specs);

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }



        // Страница для просмотра входящих заказов с поиском и сортировкой
        public async Task<IActionResult> IncomingOrders(string searchString, string sortOrder, int? month, int? year)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["DateSortParm"] = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";

            // Значения по умолчанию — текущий месяц и год
            var currentMonth = month ?? DateTime.Now.Month;
            var currentYear = year ?? DateTime.Now.Year;

            ViewData["SelectedMonth"] = currentMonth;
            ViewData["SelectedYear"] = currentYear;

            var ordersQuery = _context.Orders
                .Where(o => !o.IsActive && !o.IsCompleted &&
                            o.OrderDate.Month == currentMonth &&
                            o.OrderDate.Year == currentYear)
                .Include(o => o.Product)
                .Include(o => o.User)
                .AsQueryable();

            var userDealInfos = _context.UserDealInfos.ToList();

            var userFullNames = userDealInfos.ToDictionary(
                u => u.UserId,
                u => $"{u.LastName} {u.FirstName} {u.MiddleName}"
            );

            // Поиск по ФИО
            if (!string.IsNullOrEmpty(searchString))
            {
                var matchingUserIds = userFullNames
                    .Where(pair => pair.Value.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToList();

                ordersQuery = ordersQuery.Where(o => matchingUserIds.Contains(o.UserId));
            }

            // Сортировка по дате
            ordersQuery = sortOrder switch
            {
                "date_desc" => ordersQuery.OrderByDescending(o => o.OrderDate),
                _ => ordersQuery.OrderBy(o => o.OrderDate)
            };

            var orders = await ordersQuery.ToListAsync();

            var viewModel = orders.Select(o => new IncomingOrderViewModel
            {
                Order = o,
                FullName = userFullNames.TryGetValue(o.UserId, out var fullName)
                    ? fullName
                    : "Нет данных"
            }).ToList();

            return View(viewModel);
        }




        public async Task<IActionResult> ActiveOrders(string searchString, string sortOrder, int? month, int? year)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["DateSortParm"] = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["SelectedMonth"] = month;
            ViewData["SelectedYear"] = year;

            var ordersQuery = _context.Orders
                .Where(o => o.IsActive && !o.IsCompleted)
                .Include(o => o.Product)
                .Include(o => o.User)
                .AsQueryable();

            var userDealInfos = await _context.UserDealInfos.ToListAsync();
            var userFullNames = userDealInfos.ToDictionary(
                u => u.UserId,
                u => $"{u.LastName} {u.FirstName} {u.MiddleName}"
            );

            // Поиск по ФИО
            if (!string.IsNullOrEmpty(searchString))
            {
                var matchingUserIds = userFullNames
                    .Where(pair => pair.Value.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToList();

                ordersQuery = ordersQuery.Where(o => matchingUserIds.Contains(o.UserId));
            }

            // Фильтрация по дате
            if (month.HasValue && year.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate.Month == month && o.OrderDate.Year == year);
            }

            // Сортировка по дате
            ordersQuery = sortOrder switch
            {
                "date_desc" => ordersQuery.OrderByDescending(o => o.OrderDate),
                _ => ordersQuery.OrderBy(o => o.OrderDate)
            };

            var orders = await ordersQuery.ToListAsync();

            var viewModel = orders.Select(o => new IncomingOrderViewModel
            {
                Order = o,
                FullName = userFullNames.TryGetValue(o.UserId, out var fullName)
                    ? fullName
                    : "Нет данных"
            }).ToList();

            return View(viewModel);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.IsActive = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(IncomingOrders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.IsCompleted = true;
            order.CompletedDate = DateTime.UtcNow;
            order.IsActive = false;

            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ActiveOrders));
        }

        public async Task<IActionResult> CompletedOrders(string searchString, string sortOrder, int? month, int? year)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["DateSortParm"] = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["SelectedMonth"] = month;
            ViewData["SelectedYear"] = year;

            var ordersQuery = _context.Orders
                .Where(o => o.IsCompleted)
                .Include(o => o.Product)
                .Include(o => o.User)
                .AsQueryable();

            var userDealInfos = await _context.UserDealInfos.ToListAsync();
            var userFullNames = userDealInfos.ToDictionary(
                u => u.UserId,
                u => $"{u.LastName} {u.FirstName} {u.MiddleName}"
            );

            // Поиск по ФИО
            if (!string.IsNullOrEmpty(searchString))
            {
                var matchingUserIds = userFullNames
                    .Where(pair => pair.Value.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToList();

                ordersQuery = ordersQuery.Where(o => matchingUserIds.Contains(o.UserId));
            }

            // Фильтрация по дате
            if (month.HasValue && year.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate.Month == month && o.OrderDate.Year == year);
            }

            // Сортировка по дате
            ordersQuery = sortOrder switch
            {
                "date_desc" => ordersQuery.OrderByDescending(o => o.OrderDate),
                _ => ordersQuery.OrderBy(o => o.OrderDate)
            };

            var orders = await ordersQuery.ToListAsync();

            var viewModel = orders.Select(o => new IncomingOrderViewModel
            {
                Order = o,
                FullName = userFullNames.TryGetValue(o.UserId, out var fullName)
                    ? fullName
                    : "Нет данных"
            }).ToList();

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult GetSpecificationTemplate(int categoryId)
        {
            // Пример шаблонов. Позже можно вынести в таблицу.
            var templates = new Dictionary<int, List<(string Name, string Group)>>()
            {
                { 1, new List<(string, string)> // Телевизоры
                { ("Диагональ экрана", "Экран"),
                    ("Ёмкость аккумулятора", "Батарея") } },
                { 2, new List<(string, string)> // Смартфоны
                    { ("Операционная система", "Основные"),
                    ("Версия ОС на момент выхода", "Основные"),
                    ("Размер экрана", "Основные"),
                    ("Разрешение экрана", "Основные"),
                    ("Технология экрана", "Основные"),
                    ("Частота обновления экрана", "Основные"),
                    ("Объем оперативной памяти", "Основные"),
                    ("Объем встроенной памяти", "Основные"),
                    ("Количество точек матрицы основной камеры", "Основные"),
                    ("Максимальное разрешение видео", "Основные"),
                    ("Беспроводная зарядка", "Основные"),
                    ("Быстрая зарядка", "Основные"),
                    ("Поддержка карт памяти", "Основные"),
                    ("Количество физических SIM-карт", "Основные"),
                    ("Формат SIM-карты", "Основные"),
                    ("Процессор", "Процессор"),
                    ("Тактовая частота процессора", "Процессор"),
                    ("Количество ядер", "Процессор"),
                    ("Материал корпуса", "Конструкция"),
                    ("Ударопрочный корпус", "Конструкция"),
                    ("Пыле- и влагозащита", "Конструкция"),
                    ("Сканер отпечатка пальца", "Конструкция"),
                    ("Расположение сканера отпечатка пальца", "Конструкция"),
                    ("Разблокировка по лицу", "Конструкция"),
                    ("Длина", "Размеры и вес"),
                    ("Ширина", "Размеры и вес"),
                    ("Толщина", "Размеры и вес"),
                    ("Вес", "Размеры и вес"),
                    ("Количество цветов экрана", "Экран"),
                    ("Разрешающая способность экрана", "Экран"),
                    ("Соотношение сторон", "Экран"),
                    ("Сенсорный экран", "Экран"),
                    ("Защита от царапин", "Экран"),
                    ("Встроенная камера", "Основная камера"),
                    ("Количество модулей камеры", "Основная камера"),
                    ("Модули камер", "Основная камера"),
                    ("Диафрагма основной камеры", "Основная камера"),
                    ("Фронтальная камера", "Фронтальная камера"),
                    ("Расположение фронтальной камеры", "Фронтальная камера"),
                    ("Диафрагма фронтальной камеры", "Фронтальная камера"),
                    ("Тип аккумулятора", "Аккумулятор"),
                    ("Емкость аккумулятора", "Аккумулятор"),
                    ("Мощность зарядки", "Аккумулятор"),
                    ("Датчики", "Другое"),
                    ("Навигация", "Другое"),
                    ("Передача данных", "Другое"),
                    ("Bluetooth", "Другое"),
                    ("Аудиовыход", "Другое"),
                    ("Wi-Fi", "Другое"),
                    ("Разъём подключения", "Другое"),
                    ("NFC", "Другое"),
                    ("Комплект поставки", "Другое"),
                    ("Изготовитель", "Другое")} },
            };

            if (!templates.ContainsKey(categoryId))
                return Json(new List<object>());

            var specs = templates[categoryId]
                .Select(s => new { Name = s.Name, Group = s.Group })
                .ToList();

            return Json(specs);
        }
    }
}
