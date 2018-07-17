using System;
using System.Collections.Generic;
using System.Linq;
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

namespace WDTA2.Controllers
{
    [Authorize(Roles = Constants.OwnerRole)]
    public class OwnerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OwnerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Show Owner Inventory
        public async Task<IActionResult> Inventory(string productName)
        {
            var query = _context.OwnerInventory.Include(x => x.Product).Select(x => x);

            if (!string.IsNullOrWhiteSpace(productName))
            {
                // Adding a where to the query to filter the data.
                query = query.Where(x => x.Product.Name.Contains(productName));

                // Storing the search into ViewBag to populate the textbox with the same value for convenience.
                ViewBag.ProductName = productName;
            }

            // Adding an order by to the query for the Product ID.
            query = query.OrderBy(x => x.Product.ProductID);

            // Passing a List<OwnerInventory> model object to the View.
            return View(await query.ToListAsync());
        }


        public async Task<IActionResult> StockRequests()
        {
            // Get all the stockrequests with store and product in each
            var query = from p in _context.StockRequest.Include(x=>x.Store).Include(x => x.Product) select p;

            query = query.OrderBy(x => x.StockRequestID);

            // Passing a List of models object to the View.
            return View(await query.ToListAsync());
        }
        
        // GET: Owner/Inventory/5
        public async Task<IActionResult> SetStockLevel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var  ownerInventory = await _context.OwnerInventory.SingleOrDefaultAsync(m => m.ProductID == id);
            if (ownerInventory == null)
            {
                return NotFound("No such product ");
            }

            var product = await _context.Product.SingleOrDefaultAsync(m => m.ProductID == id);
            ownerInventory.Product = product;

            //pass the inventory object (by id) to view
            return View(ownerInventory);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStockLevel(int id, [Bind("ProductID,Product,StockLevel")] OwnerInventory ownerInventory)
        {
            if (id != ownerInventory.ProductID)
            {
                return NotFound();
            }

            var have = _context.OwnerInventory.AsNoTracking().First(x => x.ProductID == id);
            
            //Cannot update the stock level to a lower figure (compare with current level)
            if (ownerInventory.StockLevel < have.StockLevel)
            {
                //ViewBag.Wrong = "Can not set to a LOWER stock level !";
                //return View(ownerInventory);
                return NotFound("Can not set to a LOWER stock level !");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ownerInventory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(ownerInventory.ProductID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Inventory));
            }
            return View(ownerInventory);
        }
        private bool ProductExists(int id)
        {
            return _context.Product.Any(e => e.ProductID == id);
        }



        public async Task<IActionResult> ProcessRequest(int? id)//id为StockRequestID的值
        {
            if (id == null)
            {
                return NotFound();
            }

            var stockRequest = await _context.StockRequest.SingleOrDefaultAsync(m => m.StockRequestID == id);
            if (stockRequest == null)
            {
                return NotFound();
            }
            var store = await _context.Store.SingleOrDefaultAsync(m => m.StoreID == stockRequest.StoreID);
            var product = await _context.Product.SingleOrDefaultAsync(m => m.ProductID == stockRequest.ProductID);
            var ownerInventory= await _context.OwnerInventory.SingleOrDefaultAsync(m => m.ProductID == stockRequest.ProductID);
            ViewBag.ownerInventory = ownerInventory.StockLevel;

            return View(stockRequest);
        }

        [HttpPost, ActionName("ProcessRequest")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRequestConfirmed(int id)
        {

            var stockRequest = await _context.StockRequest.SingleOrDefaultAsync(m => m.StockRequestID == id);     
            var ownerHave = await _context.OwnerInventory.SingleOrDefaultAsync(m => m.ProductID == stockRequest.ProductID);
            var storeHave = await _context.StoreInventory.SingleOrDefaultAsync(m => m.ProductID == stockRequest.ProductID && m.StoreID == stockRequest.StoreID);


            if (stockRequest.Quantity> ownerHave.StockLevel)
            {
                return NotFound("Do not have enough inventory !!!");
            }

            if (storeHave==null)//insert a new item in storeInventory table
            {
                ownerHave.StockLevel -= stockRequest.Quantity;

                StoreInventory si = new StoreInventory();
                si.StoreID = stockRequest.StoreID;
                si.ProductID = stockRequest.ProductID;
                si.StockLevel = stockRequest.Quantity;

                _context.StoreInventory.Add(si);
                _context.OwnerInventory.Update(ownerHave);
                _context.StockRequest.Remove(stockRequest);
            }
            else
            {
                storeHave.StockLevel += stockRequest.Quantity;
                ownerHave.StockLevel -= stockRequest.Quantity;
                _context.StoreInventory.Update(storeHave);
                _context.OwnerInventory.Update(ownerHave);
                _context.StockRequest.Remove(stockRequest);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(StockRequests));
        }

    }
}