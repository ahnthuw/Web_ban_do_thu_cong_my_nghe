using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_ban_do_thu_cong_my_nghe.Data;
using Web_ban_do_thu_cong_my_nghe.ViewModels.Contact;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    [AllowAnonymous]
    public class ContactController : Controller
    {
        private readonly MynghevietDbContext _db;

        public ContactController(MynghevietDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? email)
        {
            ViewData["Title"] = "Liên hệ";
            await LoadHistoryAsync(email);
            return View(new PublicContactFormVM
            {
                Email = email ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(PublicContactFormVM model)
        {
            ViewData["Title"] = "Liên hệ";

            if (!ModelState.IsValid)
            {
                await LoadHistoryAsync(model.Email);
                return View(model);
            }

            var contact = new GuestContact
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
                Subject = string.IsNullOrWhiteSpace(model.Subject) ? null : model.Subject.Trim(),
                Message = model.Message.Trim(),
                SentAt = DateTime.UtcNow,
                IsProcessed = false
            };

            _db.GuestContacts.Add(contact);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Đã nhận liên hệ của bạn. Chúng tôi sẽ phản hồi sớm nhất.";
            return RedirectToAction(nameof(Index), new { email = model.Email.Trim() });
        }

        private async Task LoadHistoryAsync(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.HistoryEmail = string.Empty;
                ViewBag.History = Array.Empty<GuestContactVM>();
                return;
            }

            var normalizedEmail = email.Trim().ToLower();
            var history = await _db.GuestContacts
                .AsNoTracking()
                .Where(c => c.Email.ToLower() == normalizedEmail)
                .OrderByDescending(c => c.SentAt)
                .Select(c => new GuestContactVM
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Email = c.Email,
                    Phone = c.Phone,
                    Subject = c.Subject,
                    Message = c.Message,
                    SentAt = c.SentAt,
                    IsProcessed = c.IsProcessed
                })
                .ToListAsync();

            ViewBag.HistoryEmail = email.Trim();
            ViewBag.History = history;
        }
    }
}
