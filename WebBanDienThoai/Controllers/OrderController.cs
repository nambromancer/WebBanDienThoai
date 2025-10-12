using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebBanDienThoai.Controllers
{
    public class OrderController : Controller
    {
        public ActionResult DeliveryAddress()
        {
            return View();
        }

        public ActionResult Payment()
        {
            return View();
        }

        public ActionResult OrderConfirmed()
        {
            return View();
        }
    }
}