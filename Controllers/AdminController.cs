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
using System.Diagnostics;
using Azure.Core;
using Microsoft.AspNetCore.Identity;
using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using Category = Beltelecom.Models.Category;

namespace Beltelecom.Controllers
{
    [Authorize(Roles = "Admin")] // Доступ только для администраторов
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStock([FromBody] StockUpdateModel model)
        {
            if (model == null || model.ProductId <= 0 || model.Amount <= 0)
            {
                return BadRequest("Неверные данные");
            }

            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
            {
                return NotFound();
            }

            product.Stock += model.Amount;
            await _context.SaveChangesAsync();

            return Json(new { newStock = product.Stock });
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
            var product = await _context.Products.FindAsync(order.ProductId);


            if (order == null)
            {
                return NotFound();
            }

            product.Stock += 1;
            // Удаляем заказ
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            // Перенаправляем обратно на страницу с заказами
            return RedirectToAction(nameof(IncomingOrders));
        }

        public IActionResult DownloadPaymentSchedule(int id)
        {
            var order = _context.Orders
                .Include(o => o.Product)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
                return NotFound();

            var fileName = $"График_платежей_заказ_{order.Id}.docx";
            var memoryStream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = new Body();

                // Заголовок
                var heading = new Paragraph(new Run(new Text($"График платежей по заказу №{order.Id}")))
                {
                    ParagraphProperties = new ParagraphProperties(
                        new Justification() { Val = JustificationValues.Center },
                        new RunProperties(new RunFonts { Ascii = "Times New Roman" }, new FontSize { Val = "20" }) // 10 pt
                    )
                };
                body.Append(heading, new Paragraph(new Run())); // пустая строка

                // Таблица
                var table = new Table();

                // Границы
                var tableProperties = new TableProperties(
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 6 },
                        new BottomBorder { Val = BorderValues.Single, Size = 6 },
                        new LeftBorder { Val = BorderValues.Single, Size = 6 },
                        new RightBorder { Val = BorderValues.Single, Size = 6 },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }
                    )
                );
                table.AppendChild(tableProperties);

                // Заголовки
                var headers = new[]
                {
            ("Номер платежа", JustificationValues.Center),
            ("Срок оплаты", JustificationValues.Center),
            ("Сумма очередного ежемесячного платежа, BYN", JustificationValues.Center),
            ("Остаток долга, BYN", JustificationValues.Center)
        };

                var headerRow = new TableRow();
                foreach (var (text, align) in headers)
                {
                    var cell = new TableCell(
                        new Paragraph(
                            new ParagraphProperties(new Justification { Val = align }),
                            new Run(
                                new RunProperties(
                                    new Bold(),
                                    new RunFonts { Ascii = "Times New Roman" },
                                    new FontSize { Val = "20" } // 10pt
                                ),
                                new Text(text)
                            )
                        )
                    );
                    headerRow.Append(cell);
                }
                table.Append(headerRow);

                // Данные
                int months = order.InstallmentMonths;
                decimal total = order.TotalAmount;
                decimal monthly = Math.Round(total / months, 2);
                decimal remaining = total;

                for (int i = 0; i < months; i++)
                {
                    var date = DateTime.Today.AddMonths(i + 1);
                    remaining -= monthly;
                    if (i == months - 1) remaining = 0;

                    var row = new TableRow();
                    // Номер платежа (по правому краю)
                    row.Append(CreateCell((i + 1).ToString(), JustificationValues.Right));
                    // Срок оплаты (по правому краю)
                    row.Append(CreateCell(date.ToString("dd.MM.yyyy"), JustificationValues.Right));
                    // Ежемесячный платёж (по центру)
                    row.Append(CreateCell(monthly.ToString("F2", CultureInfo.InvariantCulture), JustificationValues.Center));
                    // Остаток (по правому краю)
                    row.Append(CreateCell(remaining.ToString("F2", CultureInfo.InvariantCulture), JustificationValues.Right));

                    table.Append(row);
                }

                body.Append(table);
                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            memoryStream.Position = 0;
            return File(memoryStream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        // Вспомогательная функция для ячеек
        private TableCell CreateCell(string text, JustificationValues alignment)
        {
            return new TableCell(
                new Paragraph(
                    new ParagraphProperties(new Justification { Val = alignment }),
                    new Run(
                        new RunProperties(
                            new RunFonts { Ascii = "Times New Roman" },
                            new FontSize { Val = "20" }
                        ),
                        new Text(text)
                    )
                )
            );
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
                {
                    ("Диагональ экрана", "Экран"),
                    ("Разрешение экрана", "Экран"),
                    ("Smart TV", "Функционал"),
                    ("Технология экрана", "Экран"),
                    ("Частота обновления экрана", "Экран"),
                    ("Цвет корпуса", "Дизайн"),
                    ("Мощность аудиосистемы", "Аудио"),
                    ("Стандарты цифрового телевидения", "Функционал"),
                    ("Голосовое управление", "Функционал"),
                    ("Беспроводные интерфейсы", "Подключение"),
                    ("Входы/выходы", "Подключение"),
                    ("Размеры без подставки (ШxВxГ)", "Габариты"),
                    ("Размеры с подставкой (ШxВxГ)", "Габариты"),
                    ("Вес с подставкой / без подставки", "Габариты"),
                    ("Крепление", "Конструкция"),
                    ("Страна производства", "Другое"),
                    ("Гарантийный срок", "Другое"),
                    ("Изготовитель", "Другое"),
                    ("Поставщик", "Другое")
                } },
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

            // Заголовки
            worksheet.Cell(1, 1).Value = "Название";
            worksheet.Cell(1, 2).Value = "Категория";
            worksheet.Cell(1, 3).Value = "Бренд";
            worksheet.Cell(1, 4).Value = "Цена, руб.";
            worksheet.Cell(1, 5).Value = "На складе, шт.";
            worksheet.Cell(1, 6).Value = "На сумму, руб.";

            decimal totalSum = 0;

            for (int i = 0; i < products.Count; i++)
            {
                var row = i + 2;
                var product = products[i];
                decimal itemSum = product.Price * product.Stock;
                totalSum += itemSum;

                worksheet.Cell(row, 1).Value = product.Name;
                worksheet.Cell(row, 2).Value = product.Category?.Name;
                worksheet.Cell(row, 3).Value = product.Brand?.Name;
                worksheet.Cell(row, 4).Value = product.Price;
                worksheet.Cell(row, 5).Value = product.Stock;
                worksheet.Cell(row, 6).Value = itemSum;
            }

            // Запись итоговой суммы жирным
            int totalRow = products.Count + 2;
            worksheet.Cell(totalRow, 6).Value = $"Итого: {totalSum:F2} руб.";
            worksheet.Cell(totalRow, 6).Style.Font.Bold = true;

            // Автоширина столбцов
            worksheet.Columns().AdjustToContents();

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
            worksheet.Cell(1, 3).Value = "Сумма, руб";
            worksheet.Cell(1, 4).Value = "Дата заказа";
            worksheet.Cell(1, 5).Value = "Рассрочка, мес";
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
            bool isFiltered = Request.Query.ContainsKey("filtered");

            var ordersQuery = _context.Orders
                .Where(o => o.IsActive && !o.IsCompleted)
                .Include(o => o.Product)
                .Include(o => o.User)
                .AsQueryable();

            var userDealInfos = await _context.UserDealInfos.ToListAsync();
            var userFullNames = userDealInfos.ToDictionary(
                u => u.UserId,
                u => new { FullName = $"{u.LastName} {u.FirstName} {u.MiddleName}", u.PhoneNumber }
            );

            if (!string.IsNullOrEmpty(searchString))
            {
                var matchingUserIds = userFullNames
                    .Where(pair => pair.Value.FullName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToList();

                ordersQuery = ordersQuery.Where(o => matchingUserIds.Contains(o.UserId));
            }

            if (isFiltered)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate.Month == month && o.OrderDate.Year == year);
            }

            var orders = await ordersQuery.OrderBy(o => o.OrderDate).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Активные заказы");

            // Заголовки
            worksheet.Cell(1, 1).Value = "ФИО";
            worksheet.Cell(1, 2).Value = "Телефон";
            worksheet.Cell(1, 3).Value = "Товар";
            worksheet.Cell(1, 4).Value = "Сумма, руб";
            worksheet.Cell(1, 5).Value = "Выплачено, руб";
            worksheet.Cell(1, 6).Value = "Рассрочка, мес";
            worksheet.Cell(1, 7).Value = "Дата заказа";
            worksheet.Cell(1, 8).Value = "Гарантия";

            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var row = i + 2;

                var userInfo = userFullNames.TryGetValue(order.UserId, out var info)
                ? info
                : new { FullName = "Нет данных", PhoneNumber = "-" };

                var warrantyEnd = order.Product?.WarrantyMonths > 0
                    ? order.OrderDate.AddMonths(order.Product.WarrantyMonths)
                    : (DateTime?)null;

                worksheet.Cell(row, 1).Value = userInfo.FullName;
                worksheet.Cell(row, 2).Value = userInfo.PhoneNumber;
                worksheet.Cell(row, 3).Value = order.Product?.Name ?? "-";
                worksheet.Cell(row, 4).Value = order.TotalAmount;
                worksheet.Cell(row, 5).Value = order.PaidAmount;
                worksheet.Cell(row, 6).Value = order.InstallmentMonths > 0 ? order.InstallmentMonths : 0;
                worksheet.Cell(row, 7).Value = order.OrderDate.ToString("dd.MM.yyyy");
                worksheet.Cell(row, 8).Value = warrantyEnd.HasValue ? $"до {warrantyEnd:dd.MM.yyyy}" : "-";
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = (isFiltered)
                ? $"Активные заказы за {month}.{year}.xlsx"
                : "Все активные заказы.xlsx";

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
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
            worksheet.Cell(1, 3).Value = "Сумма, руб";
            worksheet.Cell(1, 4).Value = "Дата заказа";
            worksheet.Cell(1, 5).Value = "Рассрочка, мес";
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

        [HttpGet]
        public async Task<IActionResult> GetDashboardData(DateTime startDate, DateTime endDate)
        {
            var ordersInRange = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            var activeOrders = ordersInRange.Count(o => o.IsActive);
            var newOrdersThisMonth = ordersInRange.Count(o =>
                o.OrderDate.Month == DateTime.Now.Month && o.OrderDate.Year == DateTime.Now.Year);

            var averageCheck = ordersInRange.Any() ? ordersInRange.Average(o => o.TotalAmount) : 0;
            var totalRevenue = ordersInRange.Sum(o => o.TotalAmount);

            // Можем добавить и другие метрики позже

            return Json(new
            {
                activeOrders,
                newOrdersThisMonth,
                averageCheck,
                totalRevenue
                // Остальные метрики и данные для графиков добавим позже
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
        public IActionResult GetRevenueByMonth(string startDate, string endDate)
        {
            // Преобразуем строки в даты
            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);

            // Получаем заказы в указанном диапазоне
            var orders = _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end && o.IsCompleted)
                .ToList();

            // Группируем по месяцам и рассчитываем выручку
            var monthlyRevenue = orders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    YearMonth = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(g => g.YearMonth)
                .ToList();

            // Формируем результат для графика
            var result = new
            {
                months = monthlyRevenue.Select(r => r.YearMonth.ToString("MMMM yyyy")).ToList(),
                revenue = monthlyRevenue.Select(r => r.Revenue).ToList()
            };

            return Json(result);
        }

        [HttpGet]
        public IActionResult GetRevenueByCategory(string startDate, string endDate)
        {
            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);

            var categoriesRevenue = _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end && o.IsCompleted)
                .GroupBy(o => o.Product.CategoryId)
                .Select(g => new
                {
                    Category = g.FirstOrDefault().Product.Category.Name, // Используем имя категории
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(g => g.Revenue)
                .ToList();

            var result = new
            {
                labels = categoriesRevenue.Select(r => r.Category).ToList(),
                data = categoriesRevenue.Select(r => r.Revenue).ToList()
            };

            return Json(result);
        }

        [HttpGet]
        public IActionResult GetRevenueByBrand(string startDate, string endDate)
        {
            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);

            var brandsRevenue = _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end && o.IsCompleted)
                .GroupBy(o => o.Product.BrandId)
                .Select(g => new
                {
                    Brand = g.FirstOrDefault().Product.Brand.Name, // Используем имя бренда
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(g => g.Revenue)
                .ToList();

            var result = new
            {
                labels = brandsRevenue.Select(r => r.Brand).ToList(),
                data = brandsRevenue.Select(r => r.Revenue).ToList()
            };

            return Json(result);
        }

        // Метод для получения количества товаров на складе по категориям
        public JsonResult GetStockByCategory(DateTime startDate, DateTime endDate)
        {
            var stockByCategory = _context.Products
                .GroupBy(p => p.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Stock = g.Sum(p => p.Stock)  // Суммируем количество товаров на складе
                })
                .ToList();

            return Json(new { labels = stockByCategory.Select(x => x.Category).ToList(), data = stockByCategory.Select(x => x.Stock).ToList() });
        }

        // Метод для получения количества товаров на складе по брендам
        public JsonResult GetStockByBrand(DateTime startDate, DateTime endDate)
        {
            var stockByBrand = _context.Products
                .GroupBy(p => p.Brand.Name)
                .Select(g => new
                {
                    Brand = g.Key,
                    Stock = g.Sum(p => p.Stock)  // Суммируем количество товаров на складе
                })
                .ToList();

            return Json(new { labels = stockByBrand.Select(x => x.Brand).ToList(), data = stockByBrand.Select(x => x.Stock).ToList() });
        }

        [HttpGet]
        public IActionResult GetWeeklyOrders(DateTime endDate)
        {
            DateTime startDate = endDate.Date.AddDays(-6);

            var days = Enumerable.Range(0, 7)
                .Select(offset => startDate.AddDays(offset))
                .ToList();

            var orders = _context.Orders
                .Where(o => o.OrderDate.Date >= startDate && o.OrderDate.Date <= endDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var result = new
            {
                labels = days.Select(d => d.ToString("dd.MM")),
                values = days.Select(d =>
                    orders.FirstOrDefault(o => o.Date.Date == d.Date)?.Count ?? 0
                )
            };
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> ExportDashboardToExcel(DateTime startDate, DateTime endDate)
        {
            var ordersInRange = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            var activeOrders = ordersInRange.Count(o => o.IsActive);
            var newOrdersThisMonth = ordersInRange.Count(o =>
                o.OrderDate.Month == DateTime.Now.Month && o.OrderDate.Year == DateTime.Now.Year);

            var averageCheck = ordersInRange.Any()
                ? (ordersInRange.Average(o => o.TotalAmount).ToString("F2") + " руб.")
                : "0.00 руб.";
            var totalRevenue = ordersInRange.Any()
                ? (ordersInRange.Sum(o => o.TotalAmount).ToString("F2") + " руб.")
                : "0.00 руб.";
            var completedOrders = ordersInRange.Where(o => o.IsCompleted).ToList();

            var monthlyRevenueRaw = completedOrders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    YearMonth = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(g => g.YearMonth)
                .ToList();
            var monthlyRevenue = monthlyRevenueRaw
                .Select(g => new
                {
                    g.YearMonth,
                    Revenue = g.Revenue.ToString("F2")
                })
                .ToList();

            var categoriesRevenueRaw = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.IsCompleted)
                .GroupBy(o => o.Product.CategoryId)
                .Select(g => new
                {
                    Category = g.FirstOrDefault().Product.Category.Name,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(g => g.Revenue)
                .ToListAsync();
            // форматирование после выборки
            var categoriesRevenue = categoriesRevenueRaw
                .Select(g => new
                {
                    g.Category,
                    Revenue = g.Revenue.ToString("F2")
                })
                .ToList();

            var brandsRevenueRaw = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.IsCompleted)
                .GroupBy(o => o.Product.BrandId)
                .Select(g => new
                {
                    Brand = g.FirstOrDefault().Product.Brand.Name,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(g => g.Revenue)
                .ToListAsync();
            var brandsRevenue = brandsRevenueRaw
                .Select(g => new
                {
                    g.Brand,
                    Revenue = g.Revenue.ToString("F2")
                })
                .ToList();

            var stockByCategory = await _context.Products
                .GroupBy(p => p.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Stock = g.Sum(p => p.Stock)
                })
                .ToListAsync();

            var stockByBrand = await _context.Products
                .GroupBy(p => p.Brand.Name)
                .Select(g => new
                {
                    Brand = g.Key,
                    Stock = g.Sum(p => p.Stock)
                })
                .ToListAsync();

            // Данные за последнюю неделю
            var startOfWeek = endDate.AddDays(-6);
            var weeklyOrders = await _context.Orders
                .Where(o => o.OrderDate.Date >= startOfWeek && o.OrderDate.Date <= endDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var weeklyLabels = Enumerable.Range(0, 7)
                .Select(offset => startOfWeek.AddDays(offset).ToString("dd.MM"))
                .ToList();

            var weeklyValues = weeklyLabels.Select(label =>
                weeklyOrders.FirstOrDefault(o => o.Date.ToString("dd.MM") == label)?.Count ?? 0
            ).ToList();

            using var workbook = new XLWorkbook();

            // Лист 1: метрики
            var metricsSheet = workbook.Worksheets.Add("Метрики");
            metricsSheet.Cell(1, 1).Value = "Период";
            metricsSheet.Cell(1, 2).Value = $"{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
            metricsSheet.Range("A1:B1").Style.Font.SetBold(true);

            metricsSheet.Cell(3, 1).Value = "Метрика";
            metricsSheet.Cell(3, 2).Value = "Значение";

            metricsSheet.Cell(4, 1).Value = "Активные заказы";
            metricsSheet.Cell(4, 2).Value = activeOrders;

            metricsSheet.Cell(5, 1).Value = "Новые заказы в этом месяце";
            metricsSheet.Cell(5, 2).Value = newOrdersThisMonth;

            metricsSheet.Cell(6, 1).Value = "Средний чек";
            metricsSheet.Cell(6, 2).Value = averageCheck;

            metricsSheet.Cell(7, 1).Value = "Общая выручка";
            metricsSheet.Cell(7, 2).Value = totalRevenue;

            // Лист 2: выручка по месяцам
            var revenueSheet = workbook.Worksheets.Add("Выручка по месяцам");
            revenueSheet.Cell(1, 1).Value = "Дата";
            revenueSheet.Cell(1, 2).Value = "Выручка, руб.";
            revenueSheet.Range("A1:B1").Style.Font.SetBold(true);

            for (int i = 0; i < monthlyRevenue.Count; i++)
            {
                revenueSheet.Cell(i + 2, 1).Value = monthlyRevenue[i].YearMonth.ToString("MMMM yyyy");
                revenueSheet.Cell(i + 2, 2).Value = monthlyRevenue[i].Revenue;
            }

            // Лист 3: выручка по категориям
            var categoryRevenueSheet = workbook.Worksheets.Add("Выручка по категориям");
            categoryRevenueSheet.Cell(1, 1).Value = "Категория";
            categoryRevenueSheet.Cell(1, 2).Value = "Выручка, руб.";
            // Сделать заголовки жирными
            categoryRevenueSheet.Range("A1:B1").Style.Font.SetBold(true);

            for (int i = 0; i < categoriesRevenue.Count; i++)
            {
                categoryRevenueSheet.Cell(i + 2, 1).Value = categoriesRevenue[i].Category;
                categoryRevenueSheet.Cell(i + 2, 2).Value = categoriesRevenue[i].Revenue;
            }

            // Лист 4: выручка по брендам
            var brandRevenueSheet = workbook.Worksheets.Add("Выручка по брендам");
            brandRevenueSheet.Cell(1, 1).Value = "Бренд";
            brandRevenueSheet.Cell(1, 2).Value = "Выручка, руб.";
            brandRevenueSheet.Range("A1:B1").Style.Font.SetBold(true);

            for (int i = 0; i < brandsRevenue.Count; i++)
            {
                brandRevenueSheet.Cell(i + 2, 1).Value = brandsRevenue[i].Brand;
                brandRevenueSheet.Cell(i + 2, 2).Value = brandsRevenue[i].Revenue;
            }

            // Лист 5: товары на складе по категориям
            var stockByCategorySheet = workbook.Worksheets.Add("Запасы (по категориям)");
            stockByCategorySheet.Cell(1, 1).Value = "Категория";
            stockByCategorySheet.Cell(1, 2).Value = "Количество товаров, шт.";
            stockByCategorySheet.Range("A1:B1").Style.Font.SetBold(true);

            for (int i = 0; i < stockByCategory.Count; i++)
            {
                stockByCategorySheet.Cell(i + 2, 1).Value = stockByCategory[i].Category;
                stockByCategorySheet.Cell(i + 2, 2).Value = stockByCategory[i].Stock;
            }

            // Лист 6: товары на складе по брендам
            var stockByBrandSheet = workbook.Worksheets.Add("Запасы (по брендам)");
            stockByBrandSheet.Cell(1, 1).Value = "Бренд";
            stockByBrandSheet.Cell(1, 2).Value = "Количество товаров, шт.";
            stockByBrandSheet.Range("A1:B1").Style.Font.SetBold(true);

            for (int i = 0; i < stockByBrand.Count; i++)
            {
                stockByBrandSheet.Cell(i + 2, 1).Value = stockByBrand[i].Brand;
                stockByBrandSheet.Cell(i + 2, 2).Value = stockByBrand[i].Stock;
            }

            // Лист 7: заказы за последнюю неделю
            var weeklyOrdersSheet = workbook.Worksheets.Add("Заказы за неделю");
            weeklyOrdersSheet.Cell(1, 1).Value = "Дата";
            weeklyOrdersSheet.Cell(1, 2).Value = "Количество заказов";
            weeklyOrdersSheet.Range("A1:B1").Style.Font.SetBold(true);

            for (int i = 0; i < weeklyLabels.Count; i++)
            {
                weeklyOrdersSheet.Cell(i + 2, 1).Value = weeklyLabels[i];
                weeklyOrdersSheet.Cell(i + 2, 2).Value = weeklyValues[i];
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Статистика с_{startDate:yyyyMMdd}по_{endDate:yyyyMMdd}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
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

        [HttpGet]
        public JsonResult GetTopProducts()
        {
            var topProducts = _context.Orders
                .Where(o => o.Product != null)
                .GroupBy(o => o.Product.Name)
                .Select(g => new
                {
                    ProductName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            return Json(new
            {
                labels = topProducts.Select(x => x.ProductName),
                counts = topProducts.Select(x => x.Count)
            });
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

        public async Task<IActionResult> UserList(string? search, string? role)
        {
            var users = await _context.Users.ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();

            var dealInfos = await _context.UserDealInfos
                .Where(info => userIds.Contains(info.UserId))
                .ToListAsync();

            var addresses = await _context.UserAddresses
                .Where(addr => userIds.Contains(addr.UserId))
                .ToListAsync();

            var roles = await _context.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id,
                    (ur, r) => new { ur.UserId, r.Name })
                .ToListAsync();

            var ordersCount = await _context.Orders
                .Where(o => userIds.Contains(o.UserId))
                .GroupBy(o => o.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();

            var model = users.Select(u =>
            {
                var dealInfo = dealInfos.FirstOrDefault(d => d.UserId == u.Id);
                var address = addresses.FirstOrDefault(a => a.UserId == u.Id);
                var roleName = roles.FirstOrDefault(r => r.UserId == u.Id)?.Name ?? "Нет";
                var count = ordersCount.FirstOrDefault(c => c.UserId == u.Id)?.Count ?? 0;

                return new UserListViewModel
                {
                    UserId = u.Id,
                    FullName = dealInfo != null
                        ? $"{dealInfo.LastName} {dealInfo.FirstName} {dealInfo.MiddleName}".Trim()
                        : "Не указано",
                    Email = u.Email,
                    PhoneNumber = dealInfo?.PhoneNumber,
                    Address = address != null
                        ? $"{address.Region}, {address.City}, {address.Street}, д.{address.House}" +
                          (string.IsNullOrWhiteSpace(address.Apartment) ? "" : $", кв.{address.Apartment}")
                        : "Не указано",
                    OrdersCount = count,
                    Role = roleName
                };
            }).ToList();

            // Фильтрация по роли, если указана
            if (!string.IsNullOrWhiteSpace(role))
            {
                model = model.Where(u => u.Role.Contains(role, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Фильтрация по поисковому запросу
            if (!string.IsNullOrWhiteSpace(search))
            {
                model = model.Where(u => u.FullName.ToLower().Contains(search.ToLower())).ToList();
            }

            // Передаем параметры фильтра в представление для сохранения состояния
            ViewData["Search"] = search;
            ViewData["Role"] = role;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole([FromBody] ChangeUserRoleDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var resultRemove = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!resultRemove.Succeeded) return BadRequest();

            var resultAdd = await _userManager.AddToRoleAsync(user, dto.Role);
            if (!resultAdd.Succeeded) return BadRequest();

            return Ok();
        }
        public async Task<IActionResult> ExportUsersToExcel(string? search, string? role)
        {
            var usersQuery = _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    DealInfo = _context.UserDealInfos.FirstOrDefault(info => info.UserId == u.Id),
                    Address = _context.UserAddresses.FirstOrDefault(addr => addr.UserId == u.Id),
                    OrdersCount = _context.Orders.Count(o => o.UserId == u.Id),
                    Role = _context.UserRoles
                                .Where(ur => ur.UserId == u.Id)
                                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                                .FirstOrDefault()
                });

            // Фильтрация по роли, если указана
            if (!string.IsNullOrWhiteSpace(role))
            {
                usersQuery = usersQuery.Where(u => u.Role == role);
            }

            // Фильтрация по поисковому запросу
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
            worksheet.Cell(1, 6).Value = "Роль";

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
                worksheet.Cell(row, 6).Value = u.Role switch
                {
                    "Admin" => "Админ",
                    "User" => "Пользователь",
                    _ => "Нет"
                };

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
