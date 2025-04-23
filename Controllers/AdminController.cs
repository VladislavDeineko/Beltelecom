using Microsoft.AspNetCore.Mvc;
using Beltelecom.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Beltelecom.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Beltelecom.Models.ViewModels;
using static NuGet.Packaging.PackagingConstants;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile imageUrl)
        {
            if (ModelState.IsValid)
            {
                // Обрабатываем изображение
                if (imageUrl != null && imageUrl.Length > 0)
                {
                    // Генерация уникального имени для файла
                    var fileName = Path.GetFileName(imageUrl.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", fileName);

                    // Сохраняем файл
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageUrl.CopyToAsync(fileStream);
                    }

                    // Устанавливаем URL изображения
                    product.ImageUrl = $"/images/{fileName}";
                }

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
                    : "Нет данных",
                PhoneNumber = userDealInfos.FirstOrDefault(u => u.UserId == o.UserId)?.PhoneNumber ?? "Не указан"
            }).ToList();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Удаляем заказ
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            // Перенаправляем обратно на страницу с заказами
            return RedirectToAction(nameof(IncomingOrders));
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
                    : "Нет данных",
                PhoneNumber = userDealInfos.FirstOrDefault(u => u.UserId == o.UserId)?.PhoneNumber ?? "Не указан"
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
                "date_desc" => ordersQuery.OrderByDescending(o => o.CompletedDate),
                _ => ordersQuery.OrderBy(o => o.CompletedDate)
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

        public IActionResult ExportToExcel(string? search, int? category, int? brand)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var normalized = search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(normalized) ||
                    p.Category.Name.ToLower().Contains(normalized) ||
                    p.Brand.Name.ToLower().Contains(normalized));
            }

            if (category.HasValue)
                query = query.Where(p => p.CategoryId == category.Value);

            if (brand.HasValue)
                query = query.Where(p => p.BrandId == brand.Value);

            var products = query.ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Товары");

            worksheet.Cell(1, 1).Value = "Название";
            worksheet.Cell(1, 2).Value = "Категория";
            worksheet.Cell(1, 3).Value = "Бренд";
            worksheet.Cell(1, 4).Value = "Цена";
            worksheet.Cell(1, 5).Value = "На складе";

            for (int i = 0; i < products.Count; i++)
            {
                var row = i + 2;
                worksheet.Cell(row, 1).Value = products[i].Name;
                worksheet.Cell(row, 2).Value = products[i].Category?.Name;
                worksheet.Cell(row, 3).Value = products[i].Brand?.Name;
                worksheet.Cell(row, 4).Value = products[i].Price;
                worksheet.Cell(row, 5).Value = products[i].Stock;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Товары.xlsx");
        }

        public IActionResult ExportIncomingOrders(string searchString, int? month, int? year)
        {
            var currentMonth = month ?? DateTime.Now.Month;
            var currentYear = year ?? DateTime.Now.Year;

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

            var orders = ordersQuery.OrderByDescending(o => o.OrderDate).ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Входящие заказы");

            worksheet.Cell(1, 1).Value = "ФИО";
            worksheet.Cell(1, 2).Value = "Товар";
            worksheet.Cell(1, 3).Value = "Сумма";
            worksheet.Cell(1, 4).Value = "Дата заказа";
            worksheet.Cell(1, 5).Value = "Рассрочка";
            worksheet.Cell(1, 6).Value = "Настройка";

            for (int i = 0; i < orders.Count; i++)
            {
                var row = i + 2;
                var order = orders[i];
                var fullName = userFullNames.TryGetValue(order.UserId, out var name) ? name : "Нет данных";

                worksheet.Cell(row, 1).Value = fullName;
                worksheet.Cell(row, 2).Value = order.Product?.Name ?? "Неизвестно";
                worksheet.Cell(row, 3).Value = order.TotalAmount;
                worksheet.Cell(row, 4).Value = order.OrderDate.ToString("dd.MM.yyyy");
                worksheet.Cell(row, 5).Value = order.InstallmentMonths > 0
                    ? $"{order.InstallmentMonths} мес."
                    : "-";
                worksheet.Cell(row, 6).Value = order.RequiresSetup ? "Да" : "Нет";
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Входящие заказы за {month}.{year}.xlsx");
        }

        public async Task<IActionResult> ExportActiveOrders(string searchString, int? month, int? year)
        {
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

            if (!string.IsNullOrEmpty(searchString))
            {
                var matchingUserIds = userFullNames
                    .Where(pair => pair.Value.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToList();

                ordersQuery = ordersQuery.Where(o => matchingUserIds.Contains(o.UserId));
            }

            if (month.HasValue && year.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate.Month == month && o.OrderDate.Year == year);
            }

            var orders = await ordersQuery.OrderBy(o => o.OrderDate).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Активные заказы");

            worksheet.Cell(1, 1).Value = "ФИО";
            worksheet.Cell(1, 2).Value = "Товар";
            worksheet.Cell(1, 3).Value = "Сумма";
            worksheet.Cell(1, 4).Value = "Дата заказа";
            worksheet.Cell(1, 5).Value = "Рассрочка";
            worksheet.Cell(1, 6).Value = "Настройка";

            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var fullName = userFullNames.TryGetValue(order.UserId, out var name) ? name : "Нет данных";

                worksheet.Cell(i + 2, 1).Value = fullName;
                worksheet.Cell(i + 2, 2).Value = order.Product?.Name ?? "";
                worksheet.Cell(i + 2, 3).Value = $"{order.TotalAmount:0.00} руб.";
                worksheet.Cell(i + 2, 4).Value = order.OrderDate.ToString("dd.MM.yyyy");
                worksheet.Cell(i + 2, 5).Value = order.InstallmentMonths > 0 ? $"{order.InstallmentMonths} мес." : "-";
                worksheet.Cell(i + 2, 6).Value = order.RequiresSetup ? "Да" : "Нет";
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Активные заказы за {month}.{year}.xlsx");
        }

        public async Task<IActionResult> ExportCompletedOrders(string searchString, int? month, int? year)
        {
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

            if (!string.IsNullOrEmpty(searchString))
            {
                var matchingUserIds = userFullNames
                    .Where(pair => pair.Value.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToList();

                ordersQuery = ordersQuery.Where(o => matchingUserIds.Contains(o.UserId));
            }

            if (month.HasValue && year.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate.Month == month && o.OrderDate.Year == year);
            }

            var orders = await ordersQuery.OrderBy(o => o.OrderDate).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Завершённые заказы");

            worksheet.Cell(1, 1).Value = "ФИО";
            worksheet.Cell(1, 2).Value = "Товар";
            worksheet.Cell(1, 3).Value = "Сумма";
            worksheet.Cell(1, 4).Value = "Дата заказа";
            worksheet.Cell(1, 5).Value = "Рассрочка";
            worksheet.Cell(1, 6).Value = "Настройка";

            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var fullName = userFullNames.TryGetValue(order.UserId, out var name) ? name : "Нет данных";

                worksheet.Cell(i + 2, 1).Value = fullName;
                worksheet.Cell(i + 2, 2).Value = order.Product?.Name ?? "";
                worksheet.Cell(i + 2, 3).Value = $"{order.TotalAmount:0.00} руб.";
                worksheet.Cell(i + 2, 4).Value = order.OrderDate.ToString("dd.MM.yyyy");
                worksheet.Cell(i + 2, 5).Value = order.InstallmentMonths > 0 ? $"{order.InstallmentMonths} мес." : "-";
                worksheet.Cell(i + 2, 6).Value = order.RequiresSetup ? "Да" : "Нет";
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                 $"Завершенные заказы за {month}.{year}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> GetOrdersByMonthTypes(int startMonth, int startYear, int endMonth, int endYear)
        {
            // Запрос для получения всех заказов за указанный период
            var ordersByPeriod = await _context.Orders
                .Where(o => (o.OrderDate.Year > startYear || (o.OrderDate.Year == startYear && o.OrderDate.Month >= startMonth)) &&
                            (o.OrderDate.Year < endYear || (o.OrderDate.Year == endYear && o.OrderDate.Month <= endMonth)))
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalOrders = g.Count(),
                    ActiveOrders = g.Count(o => o.IsActive && !o.IsCompleted),
                    CompletedOrders = g.Count(o => o.IsCompleted)
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            var months = ordersByPeriod.Select(o => $"{o.Month}-{o.Year}").ToList();
            var totalOrders = ordersByPeriod.Select(o => o.TotalOrders).ToList();
            var activeOrders = ordersByPeriod.Select(o => o.ActiveOrders).ToList();
            var completedOrders = ordersByPeriod.Select(o => o.CompletedOrders).ToList();

            // Возвращаем данные в формате JSON
            return Json(new
            {
                months,
                totalOrders,
                activeOrders,
                completedOrders
            });
        }


        public async Task<IActionResult> Statistics()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var activeOrders = await _context.Orders.CountAsync(o => o.IsActive && !o.IsCompleted);
            var completedOrders = await _context.Orders.CountAsync(o => o.IsCompleted);
            var newOrdersThisMonth = await _context.Orders.CountAsync(o => o.OrderDate.Month == DateTime.Now.Month && o.OrderDate.Year == DateTime.Now.Year);

            var ordersByMonth = await _context.Orders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            var averageOrderAmount = await _context.Orders
                .Where(o => o.IsCompleted)
                .AverageAsync(o => o.PaidAmount);

            var months = ordersByMonth.Select(o => $"{o.Month}-{o.Year}").ToList();
            var ordersCount = ordersByMonth.Select(o => o.Count).ToList();

            var model = new DashboardStatsViewModel
            {
                TotalOrders = totalOrders,
                ActiveOrders = activeOrders,
                CompletedOrders = completedOrders,
                NewOrdersThisMonth = newOrdersThisMonth,
                AverageOrderAmount = averageOrderAmount,
                Months = months,
                OrdersByMonth = ordersCount
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrdersByCategory()
        {
            var data = await _context.Orders
                .Where(o => o.Product.CategoryId != null)
                .GroupBy(o => o.Product.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Count)
                .ToListAsync();

            var labels = data.Select(d => d.Category).ToList();
            var counts = data.Select(d => d.Count).ToList();

            return Json(new { labels, counts });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrdersByBrand()
        {
            var brandOrders = await _context.Orders
                .Where(o => o.Product != null && o.Product.BrandId != null)
                .GroupBy(o => o.Product.Brand.Name)
                .Select(g => new
                {
                    Brand = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Count)
                .ToListAsync();

            var labels = brandOrders.Select(x => x.Brand).ToList();
            var counts = brandOrders.Select(x => x.Count).ToList();

            return Json(new { labels, counts });
        }

        //Категории/бренды
        public IActionResult ManageCategoriesAndBrands()
        {
            var categories = _context.Categories.ToList();
            var brands = _context.Brands.ToList();

            ViewBag.Categories = categories;
            ViewBag.Brands = brands;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string name, IFormFile? image)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var category = new Category { Name = name.Trim() };

                // Если загружено изображение
                if (image != null && image.Length > 0)
                {
                    // Генерация уникального имени файла
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    var imagePath = Path.Combine("wwwroot", "images");

                    // Убедимся, что папка существует
                    if (!Directory.Exists(imagePath))
                    {
                        Directory.CreateDirectory(imagePath);
                    }

                    // Полный путь к файлу
                    var fullPath = Path.Combine(imagePath, fileName);

                    // Сохраняем файл
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    // Сохраняем относительный путь в БД
                    category.ImageUrl = $"/images/{fileName}";
                }

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ManageCategoriesAndBrands));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBrand(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _context.Brands.Add(new Brand { Name = name.Trim() });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageCategoriesAndBrands));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                TempData["Error"] = "Категория не найдена.";
                return RedirectToAction(nameof(ManageCategoriesAndBrands));
            }

            if (category.Products.Any())
            {
                TempData["Error"] = "Нельзя удалить категорию, к которой привязаны товары.";
                return RedirectToAction(nameof(ManageCategoriesAndBrands));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageCategoriesAndBrands));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _context.Brands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
            {
                TempData["Error"] = "Бренд не найден.";
                return RedirectToAction(nameof(ManageCategoriesAndBrands));
            }

            if (brand.Products.Any())
            {
                TempData["Error"] = "Нельзя удалить бренд, к которому привязаны товары.";
                return RedirectToAction(nameof(ManageCategoriesAndBrands));
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageCategoriesAndBrands));
        }

        public async Task<IActionResult> UserList(string? search)
        {
            var usersQuery = _context.Users.Select(u => new
            {
                u.Id,
                u.Email,
                DealInfo = _context.UserDealInfos.FirstOrDefault(info => info.UserId == u.Id),
                Address = _context.UserAddresses.FirstOrDefault(addr => addr.UserId == u.Id),
                OrdersCount = _context.Orders.Count(o => o.UserId == u.Id)
            });

            if (!string.IsNullOrWhiteSpace(search))
            {
                usersQuery = usersQuery.Where(u =>
                    (u.DealInfo!.LastName + " " + u.DealInfo.FirstName + " " + u.DealInfo.MiddleName)
                    .ToLower().Contains(search.ToLower()));
            }

            var users = await usersQuery.ToListAsync();

            var model = users.Select(u => new UserListViewModel
            {
                FullName = $"{u.DealInfo?.LastName} {u.DealInfo?.FirstName} {u.DealInfo?.MiddleName}".Trim(),
                Email = u.Email,
                PhoneNumber = u.DealInfo?.PhoneNumber,
                Address = u.Address != null
                    ? $"{u.Address.Region}, {u.Address.City}, {u.Address.Street}, д.{u.Address.House}" +
                      (string.IsNullOrWhiteSpace(u.Address.Apartment) ? "" : $", кв.{u.Address.Apartment}")
                    : "Не указано",
                OrdersCount = u.OrdersCount
            }).ToList();

            return View(model);
        }

        public async Task<IActionResult> ExportUsersToExcel(string? search)
        {
            var usersQuery = _context.Users.Select(u => new
            {
                u.Id,
                u.Email,
                DealInfo = _context.UserDealInfos.FirstOrDefault(info => info.UserId == u.Id),
                Address = _context.UserAddresses.FirstOrDefault(addr => addr.UserId == u.Id),
                OrdersCount = _context.Orders.Count(o => o.UserId == u.Id)
            });

            if (!string.IsNullOrWhiteSpace(search))
            {
                usersQuery = usersQuery.Where(u =>
                    (u.DealInfo!.LastName + " " + u.DealInfo.FirstName + " " + u.DealInfo.MiddleName)
                    .ToLower().Contains(search.ToLower()));
            }

            var users = await usersQuery.ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Пользователи");

            worksheet.Cell(1, 1).Value = "ФИО";
            worksheet.Cell(1, 2).Value = "Email";
            worksheet.Cell(1, 3).Value = "Телефон";
            worksheet.Cell(1, 4).Value = "Адрес";
            worksheet.Cell(1, 5).Value = "Кол-во заказов";

            int row = 2;
            foreach (var u in users)
            {
                string fullName = $"{u.DealInfo?.LastName} {u.DealInfo?.FirstName} {u.DealInfo?.MiddleName}".Trim();
                string address = u.Address != null
                    ? $"{u.Address.Region}, {u.Address.City}, {u.Address.Street}, д.{u.Address.House}" +
                      (string.IsNullOrWhiteSpace(u.Address.Apartment) ? "" : $", кв.{u.Address.Apartment}")
                    : "Не указано";

                worksheet.Cell(row, 1).Value = fullName;
                worksheet.Cell(row, 2).Value = u.Email;
                worksheet.Cell(row, 3).Value = u.DealInfo?.PhoneNumber;
                worksheet.Cell(row, 4).Value = address;
                worksheet.Cell(row, 5).Value = u.OrdersCount;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"Пользователи_{DateTime.Now:yyyy.MM.dd_HH:mm}.xlsx";
            return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
        }

    }
}
