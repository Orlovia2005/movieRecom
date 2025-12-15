using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using plt.Migrations;
using plt.Models.Model;
using plt.Models.ViewModel;
using System.Threading.Tasks;

namespace plt.Controllers
{
    public class ClientController : BaseController
    {
        public ClientController(EducationDbContext context) : base(context) { }

        [HttpGet]
        public async Task<IActionResult> Client()
        {
            var clients = await _context.Clients
                .Include(s => s.LastDate)
                .Include(s => s.Profi)
                .Where(p => p.ProfiId == (int)CurrentUserId)
                .ToListAsync();

            var viewClient = clients.Select(c => new ClientViewModel
            {
                Id = c.Id,
                Name = c.Name,
                SecondName = c.SecondName,
                Email = c.Email,
                Phone = c.Phone,
                CountServicesDone = c.CountServicesDone,
                PriceServices = c.PriceServices,
                Balance = c.Balance,
                Profi = c.Profi,
                ProfiId = c.ProfiId,
                LastDate = c.LastDate,
                LastDateId = c.LastDateId
            }).ToList();

            return View(viewClient);
        }

        [HttpPost]
        public async Task<IActionResult> AddClient(ClientViewModel client)
        {
            if (client == null)
            {
                Notif_Error("Заполните клиента");
                return RedirectToAction("Client");
            }

            var new_client = new Client
            {
                Name = client.Name,
                SecondName = client.SecondName,
                Email = client.Email,
                Phone = client.Phone,
                ProfiId = (int)CurrentUserId!,
                Balance = client.Balance,
                CountServicesDone = client.CountServicesDone,
                PriceServices = client.PriceServices,
                LastDateId = null
            };

            _context.Clients.Add(new_client);
            await _context.SaveChangesAsync();
            Notif_Success("Клиент успешно добавлен.");
            return RedirectToAction("Client");
        }

        [HttpPost]
        public async Task<IActionResult> MarkVisit(int clientId)
        {
            try
            {
                // Начинаем транзакцию для обеспечения целостности данных
                using var transaction = await _context.Database.BeginTransactionAsync();

                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.Id == clientId && c.ProfiId == CurrentUserId);

                if (client == null)
                {
                    Notif_Error("Клиент не найден");
                    return RedirectToAction("Client");
                }

                // Проверяем, достаточно ли средств на балансе
                if (client.Balance < client.PriceServices)
                {
                    Notif_Error($"Недостаточно средств на балансе. Требуется: {client.PriceServices}, доступно: {client.Balance}");
                    return RedirectToAction("Client");
                }

                // Создаем новую отметку о посещении
                var serviceDate = new ServicesDate
                {
                    Date = DateTime.Now,
                    ClientId = clientId,
                    Price = client.PriceServices,
                    ProfiId = (int)CurrentUserId!
                };

                _context.ServiceDates.Add(serviceDate);

                // Сохраняем чтобы получить Id для serviceDate
                await _context.SaveChangesAsync();

                // Обновляем клиента
                client.LastDateId = serviceDate.Id;
                client.CountServicesDone++;
                client.Balance -= client.PriceServices; // списываем с баланса

                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); // Подтверждаем транзакцию

                Notif_Success($"Посещение отмечено успешно! Списано: {client.PriceServices} руб.");
            }
            catch (Exception ex)
            {
                Notif_Error($"Ошибка при отметке посещения: {ex.Message}");
                // Транзакция автоматически откатится при выходе из using блока
            }

            return RedirectToAction("Client");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteClient(int clientId)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == clientId && c.ProfiId == CurrentUserId);

            if (client == null)
            {
                Notif_Error("Клиент не найден");
                return RedirectToAction("Client");
            }

            // Удаляем связанные отметки о посещениях
            var serviceDates = await _context.ServiceDates
                .Where(sd => sd.ClientId == clientId)
                .ToListAsync();

            _context.ServiceDates.RemoveRange(serviceDates);
            _context.Clients.Remove(client);

            await _context.SaveChangesAsync();
            Notif_Success("Клиент успешно удален");
            return RedirectToAction("Client");
        }

        [HttpGet]
        public async Task<IActionResult> GetClientDetails(int clientId)
        {
            var client = await _context.Clients
                .Include(c => c.LastDate)
                .Include(c => c.ServiceDates.OrderByDescending(sd => sd.Date))
                .FirstOrDefaultAsync(c => c.Id == clientId && c.ProfiId == CurrentUserId);

            if (client == null)
            {
                return Json(new { success = false, message = "Клиент не найден" });
            }

            var clientDetails = new
            {
                Id = client.Id,
                FullName = client.Name + " " + client.SecondName,
                Email = client.Email ?? "Не указан",
                Phone = client.Phone ?? "Не указан",
                Balance = client.Balance,
                PriceServices = client.PriceServices,
                CountServicesDone = client.CountServicesDone,
                LastVisit = client.LastDate?.Date.ToString("dd.MM.yyyy HH:mm") ?? "Нет посещений",
                Services = client.ServiceDates.Select(sd => new
                {
                    Date = sd.Date.ToString("dd.MM.yyyy HH:mm"),
                    Price = sd.Price
                })
            };

            return Json(new { success = true, client = clientDetails });
        }
    }
}