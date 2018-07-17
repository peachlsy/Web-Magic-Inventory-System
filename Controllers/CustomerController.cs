using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using WDTA2.Models;
using WDTA2.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using WDTA2.Models.BusinessModels;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;


namespace WDTA2.Controllers
{

    //Copy from MicroSoft official method to get complex object from json file.
    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) :
                JsonConvert.DeserializeObject<T>(value);
        }
    }

    [Authorize(Roles = "CustomerRole")]
    public class CustomerController : Controller
    {
        private static int _storeID;
        private readonly ApplicationDbContext _context;
        private static int sessionNum=0;


        public CustomerController(ApplicationDbContext context)
        {
            _context = context;

        }

        public async Task<IActionResult> Inventory(string productName,int storeID)
        {

            if (storeID!=0)
            {
                _storeID = storeID;
                
            }
            var query = _context.StoreInventory.Include(x => x.Product).Include(x => x.Store).Where(x => x.StoreID == _storeID).Select(x => x);

            if (!string.IsNullOrWhiteSpace(productName))
            {
                query = query.Where(x => x.Product.Name.Contains(productName));

                ViewBag.ProductName = productName;
            }

            query = query.OrderBy(x => x.Product.ProductID);
            ViewBag.id = _storeID;

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> AddToCart(int? id, int storeID)
        {
            if (id == null)
            {
                return NotFound();
            }

            var storeInventory = await _context.StoreInventory.SingleOrDefaultAsync(m => m.ProductID == id && m.StoreID == storeID);
            var product = await _context.Product.SingleOrDefaultAsync(m => m.ProductID == storeInventory.ProductID);
            storeInventory.Product = product;

            if (storeInventory == null||storeInventory.StockLevel<1)
            {
                return NotFound();
            }
            return View(storeInventory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id, int storeID, [Bind("ProductID,Product,StockLevel")] StoreInventory storeInventory)
        {
            if (id != storeInventory.ProductID)
            {
                return NotFound();
            }
            if (storeInventory.StockLevel < 1) {
                return NotFound("Invalid quantity");
            }
            OrderItem orderItem = new OrderItem();
            orderItem.StoreID = storeID;
            orderItem.ProductID = id;
            orderItem.Product = await _context.Product.SingleOrDefaultAsync(m => m.ProductID == id);
            orderItem.Quantity = storeInventory.StockLevel;
            ViewBag.storeID = storeID;
            var storeAlreadyHave =
                await _context.StoreInventory.SingleOrDefaultAsync(m => m.ProductID == id && m.StoreID == storeID);

            bool alreadyHave = false;
            foreach (var key in HttpContext.Session.Keys.ToList())
            {
                if ((HttpContext.Session.Get<OrderItem>(key).StoreID == orderItem.StoreID) &&
                    (HttpContext.Session.Get<OrderItem>(key).ProductID == orderItem.ProductID))
                {
                    //update item in session which has already in session
                    orderItem.Quantity = HttpContext.Session.Get<OrderItem>(key).Quantity + storeInventory.StockLevel;
                    if (orderItem.Quantity> storeAlreadyHave.StockLevel)
                    {
                        return NotFound("No enough inventory");
                    }
                    HttpContext.Session.Set(key,orderItem);
                    alreadyHave = true;
                }
            }
            if (orderItem.Quantity > storeAlreadyHave.StockLevel)
            {
                return NotFound();
            }
            if (!alreadyHave)//Add new item in session
            {
                sessionNum += 1;
                HttpContext.Session.Set(sessionNum.ToString(),orderItem);
            }
            if (ModelState.IsValid)
            {          
                return RedirectToAction(nameof(Inventory));
            }
            return View(storeInventory);
        }
        private bool ProductExists(int id)
        {
            return _context.Product.Any(e => e.ProductID == id);
        }

        
        public IActionResult ShowCart()
        {
            return View();
        }

        public async Task<IActionResult> CheckOutConfirm(Decimal totalPrice)
        {

            Order order=new Order();
            order.CreaTime=DateTime.Now;
            _context.Add(order);
            await _context.SaveChangesAsync();
            var query = _context.Order;
            int id = 0;
            DateTime maxDateTime = DateTime.MinValue;
            foreach (var thisOrder in query)
            {
                if (thisOrder.CreaTime>maxDateTime)
                {
                    maxDateTime = thisOrder.CreaTime;
                }
            }

            var currentOrder = await _context.Order.SingleOrDefaultAsync(x => x.CreaTime == maxDateTime);
            id = currentOrder.OrderID;

            foreach (var key in HttpContext.Session.Keys)
            {
                var currOrderItem = HttpContext.Session.Get<OrderItem>(key);
                currOrderItem.Product = null;
                currOrderItem.OrderID = id;
                _context.OrderItem.Add(currOrderItem);
                //delete item in storeInventory table
                var storeInventory=await _context.StoreInventory.SingleOrDefaultAsync(x =>
                    x.StoreID == currOrderItem.StoreID && x.ProductID == currOrderItem.ProductID);
                storeInventory.StockLevel -=currOrderItem.Quantity;

                await _context.SaveChangesAsync();
            }
            HttpContext.Session.Clear();

            var queryOrderItem = _context.OrderItem.Include(x => x.Product).Where(x=> x.OrderID == id).Select(x=>x);
            ViewBag.totalPrice = totalPrice;
            ViewBag.orderID = id;
            return View( await queryOrderItem.ToListAsync());
        }


        public IActionResult DeleteItem(string key)
        {
            HttpContext.Session.Remove(key);
            return RedirectToAction(nameof(ShowCart));
        }

        public IActionResult CardValidation(Decimal totalPreice)
        {
            if (!HttpContext.Session.Keys.Any())
            {
                return NotFound("No products in the cart!");
            }
            ViewBag.totalPrice = totalPreice;
            return View();
        }

        public async Task<IActionResult> ShowOrderHistory()
        {
            var response = await OrderHistoryApi.InitializeClient().GetAsync("api/WebApi");

            if (!response.IsSuccessStatusCode)
                throw new Exception();

            // Storing the response details recieved from web api.
            var result = response.Content.ReadAsStringAsync().Result;

            // Deserializing the response recieved from web api and storing into a list.
            var orderItem = JsonConvert.DeserializeObject<List<OrderItem>>(result);

            //order by OrderID
            var orders = new Dictionary<int, List<OrderItem>>();//Save OrderID and OrderItems
            var orderDate = new Dictionary<int, DateTime>();

            foreach (var item in orderItem)
            {
                item.Store = await _context.Store.SingleOrDefaultAsync(x => x.StoreID == item.StoreID);
                item.Product = await _context.Product.SingleOrDefaultAsync(x => x.ProductID == item.ProductID);

                if (orders == null || !orders.Keys.Contains(item.OrderID))
                {
                    var orderItemList = new List<OrderItem>();
                    var orderDateList = new List<DateTime>();
                    orders.Add(item.OrderID,orderItemList);
                    var order = await _context.Order.FirstOrDefaultAsync(x => x.OrderID == item.OrderID);
                    orderDate.Add(item.OrderID, order.CreaTime);
                    orderItemList.Add(item);

                }
                else
                {
                    var orderItemList = new List<OrderItem>();
                    orders.TryGetValue(item.OrderID, out orderItemList);
                    orderItemList.Add(item);
                    orders[item.OrderID] = orderItemList;
                }

            }

            ViewData["OrderDate"] = orderDate;
            ViewData["OrdersMap"] = orders;
           
            //return View(orderItem);
            return View();
        }

       

    }
}
