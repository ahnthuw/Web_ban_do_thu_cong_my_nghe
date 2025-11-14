using Microsoft.AspNetCore.Mvc;
using Web_ban_do_thu_cong_my_nghe.Helpers;
using Web_ban_do_thu_cong_my_nghe.ViewModels;

namespace Web_ban_do_thu_cong_my_nghe.ViewComponents
{
    public class CartViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
                var cart = HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();
            return View("CartPanel", new CartModel
            {
                Quantity = cart.Sum(p => p.SoLuong ),
                Total = cart.Sum(p => p.ThanhTien)
            });
        }
    }
    
}
