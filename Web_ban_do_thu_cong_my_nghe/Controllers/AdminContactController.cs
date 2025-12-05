using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_ban_do_thu_cong_my_nghe.Data;
using Web_ban_do_thu_cong_my_nghe.Helpers;
using Web_ban_do_thu_cong_my_nghe.ViewModels.Contact;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminContactController : Controller
    {
        private readonly MynghevietDbContext _db;

        public AdminContactController(MynghevietDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? customerId, bool noConversation = false)
        {
            var summaries = await BuildThreadSummariesAsync();
            var guestContacts = await BuildGuestContactSummariesAsync();
            ContactConversationVM? conversation = null;

                if (customerId.HasValue && !noConversation)
            {
                conversation = await BuildConversationAsync(customerId.Value);
            }

            // Không tự động mở hội thoại khi đang ở chế độ đóng

            var model = new AdminContactDashboardVM
            {
                Threads = summaries,
                CurrentConversation = conversation,
                    SelectedCustomerId = noConversation ? null : customerId,
                GuestContacts = guestContacts
            };

            // Nếu vừa đánh dấu đã đọc, cập nhật trạng thái hiển thị cho thread tương ứng (không thay đổi DB)
            if (TempData.ContainsKey("MarkedReadCustomerId") && int.TryParse(TempData["MarkedReadCustomerId"]?.ToString(), out var markedId))
            {
                var target = model.Threads.FirstOrDefault(t => t.CustomerId == markedId);
                if (target != null)
                {
                    target.LastFromCustomer = false;
                    target.LastMessagePreview = string.IsNullOrEmpty(target.LastMessagePreview) ? "Đã đọc" : target.LastMessagePreview;
                }
            }

            if (customerId.HasValue && (model.CurrentConversation != null || !summaries.Any()))
            {
                var exists = summaries.Any(s => s.CustomerId == customerId.Value);
                if (!exists)
                {
                    var customer = await _db.Users.FindAsync(customerId.Value);
                    if (customer != null)
                    {
                        model.Threads.Insert(0, new ContactThreadSummaryVM
                        {
                            CustomerId = customer.Id,
                            CustomerName = customer.Fullname ?? customer.Email ?? $"Khách #{customer.Id}",
                            CustomerEmail = customer.Email ?? string.Empty,
                            LastMessageAt = null,
                            LastMessagePreview = "Chưa có tin nhắn",
                            LastFromCustomer = false
                        });
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply([Bind(Prefix = "CurrentConversation.Composer")] ContactSendVM input, int customerId)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            if (!ModelState.IsValid)
            {
                var summaries = await BuildThreadSummariesAsync();
                var conversation = await BuildConversationAsync(customerId) ?? new ContactConversationVM
                {
                    PartnerId = customerId,
                    PartnerName = "Khách hàng",
                    PartnerRoleLabel = "Khách hàng",
                    Composer = input
                };

                conversation.Composer = input;

                var model = new AdminContactDashboardVM
                {
                    Threads = summaries,
                    CurrentConversation = conversation,
                    SelectedCustomerId = customerId
                };

                return View("Index", model);
            }

            var message = new ContactMessage
            {
                SenderId = adminId.Value,
                ReceiverId = customerId,
                Message = input.Message.Trim(),
                SentAt = DateTime.UtcNow,
                IsAdminMessage = true
            };

            _db.ContactMessages.Add(message);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Đã gửi phản hồi cho khách hàng.";
            return RedirectToAction(nameof(Index), new { customerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteThread(int customerId)
        {
            var messages = await _db.ContactMessages
                .Where(m => m.SenderId == customerId || m.ReceiverId == customerId)
                .ToListAsync();

            if (messages.Any())
            {
                _db.ContactMessages.RemoveRange(messages);
                await _db.SaveChangesAsync();
                TempData["StatusMessage"] = "Đã xoá hội thoại khách hàng.";
            }
            else
            {
                TempData["StatusMessage"] = "Không tìm thấy hội thoại để xoá.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkThreadRead(int customerId)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            var marker = _db.ContactThreadReads
                .FirstOrDefault(m => m.AdminId == adminId.Value && m.CustomerId == customerId);

            if (marker == null)
            {
                marker = new ContactThreadRead
                {
                    AdminId = adminId.Value,
                    CustomerId = customerId,
                    LastReadAt = DateTime.UtcNow
                };
                _db.ContactThreadReads.Add(marker);
            }
            else
            {
                marker.LastReadAt = DateTime.UtcNow;
                _db.ContactThreadReads.Update(marker);
            }

            _db.SaveChanges();

            TempData["MarkedReadCustomerId"] = customerId;
            TempData["StatusMessage"] = "Đã đánh dấu hội thoại khách hàng là đã đọc.";
            return RedirectToAction(nameof(Index), new { customerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkGuestContactProcessed(int id)
        {
            var contact = await _db.GuestContacts.FindAsync(id);
            if (contact == null)
            {
                TempData["StatusMessage"] = "Không tìm thấy liên hệ.";
                return RedirectToAction(nameof(Index));
            }

            if (!contact.IsProcessed)
            {
                contact.IsProcessed = true;
                _db.GuestContacts.Update(contact);
                await _db.SaveChangesAsync();
            }

            TempData["StatusMessage"] = "Đã đánh dấu liên hệ là đã xử lý.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGuestContact(int id)
        {
            var contact = await _db.GuestContacts.FindAsync(id);
            if (contact == null)
            {
                TempData["StatusMessage"] = "Không tìm thấy liên hệ để xoá.";
                return RedirectToAction(nameof(Index));
            }

            _db.GuestContacts.Remove(contact);
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Đã xoá liên hệ khách vãng lai.";
            return RedirectToAction(nameof(Index));
        }

        private int? GetCurrentAdminId()
        {
            var idValue = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            return int.TryParse(idValue, out var id) ? id : null;
        }

        private async Task<List<GuestContactVM>> BuildGuestContactSummariesAsync()
        {
            var contacts = await _db.GuestContacts
                .AsNoTracking()
                .OrderByDescending(c => c.SentAt)
                .ToListAsync();

            return contacts.Select(c => new GuestContactVM
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                Phone = c.Phone,
                Subject = c.Subject,
                Message = c.Message,
                SentAt = c.SentAt,
                IsProcessed = c.IsProcessed
            }).ToList();
        }

        private async Task<List<ContactThreadSummaryVM>> BuildThreadSummariesAsync()
        {
            var messages = await _db.ContactMessages
                .AsNoTracking()
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            if (!messages.Any())
            {
                return new List<ContactThreadSummaryVM>();
            }

            var customerIds = messages
                .Select(m => m.IsAdminMessage ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToList();

            var customers = await _db.Users
                .Where(u => customerIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Fullname, u.Email })
                .ToListAsync();

            var adminId = GetCurrentAdminId();
            var readMap = new Dictionary<int, DateTime?>();
            if (adminId != null)
            {
                readMap = await _db.ContactThreadReads
                    .Where(r => r.AdminId == adminId.Value)
                    .ToDictionaryAsync(r => r.CustomerId, r => (DateTime?)r.LastReadAt);
            }

            var summaries = messages
                .GroupBy(m => m.IsAdminMessage ? m.ReceiverId : m.SenderId)
                .Select(g =>
                {
                    var last = g.OrderByDescending(x => x.SentAt).First();
                    var customer = customers.FirstOrDefault(c => c.Id == g.Key);
                    var lastReadAt = readMap.TryGetValue(g.Key, out var t) ? t : null;
                    var lastFromCustomer = !last.IsAdminMessage;
                    var isUnread = lastFromCustomer && (lastReadAt == null || last.SentAt > lastReadAt.Value);
                    return new ContactThreadSummaryVM
                    {
                        CustomerId = g.Key,
                        CustomerName = customer?.Fullname ?? customer?.Email ?? $"Khách #{g.Key}",
                        CustomerEmail = customer?.Email ?? string.Empty,
                        LastMessageAt = last.SentAt,
                        LastMessagePreview = last.Message.Length > 80 ? string.Concat(last.Message.AsSpan(0, 80), "...") : last.Message,
                        LastFromCustomer = isUnread
                    };
                })
                .OrderByDescending(s => s.LastMessageAt)
                .ToList();

            return summaries;
        }

        private async Task<ContactConversationVM?> BuildConversationAsync(int customerId)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == null)
            {
                return null;
            }

            var customer = await _db.Users.FindAsync(customerId);
            if (customer == null)
            {
                return null;
            }

            var messages = await _db.ContactMessages
                .AsNoTracking()
                .Where(m => (m.SenderId == customerId && m.ReceiverId == adminId.Value) || (m.SenderId == adminId.Value && m.ReceiverId == customerId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return new ContactConversationVM
            {
                PartnerId = customerId,
                PartnerName = customer.Fullname ?? customer.Email ?? $"Khách #{customerId}",
                PartnerRoleLabel = "Khách hàng",
                Messages = messages.Select(m => new ContactMessageVM
                {
                    Id = m.Id,
                    Content = m.Message,
                    SentAt = m.SentAt,
                    IsOwn = m.IsAdminMessage,
                    IsAdminMessage = m.IsAdminMessage
                }).ToList(),
                Composer = new ContactSendVM()
            };
        }
    }
}
