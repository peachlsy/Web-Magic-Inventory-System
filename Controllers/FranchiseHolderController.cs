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


namespace WDTA2.Controllers
{

    [Authorize(Roles = "FranchHolderCBD, FranchHolderNorth, FranchHolderSouth, FranchHolderEast, FranchHolderWest")]
    public class FranchiseHolderController : Controller
    {
        private static int _storeID;
        private readonly ApplicationDbContext _context;



        public FranchiseHolderController(ApplicationDbContext context)
        {
            _context = context;

        }

        public int GetStoreID()
        {
            int id = 0;

            if (HttpContext.User.IsInRole(Constants.FranchHolderRoleCbd))
            {
                id = 1;
            }
            else if (HttpContext.User.IsInRole(Constants.FranchHolderRoleNorth))
            {
                id = 2;
            }
            else if (HttpContext.User.IsInRole(Constants.FranchHolderRoleEast))
            {
                id = 3;
            }
            else if (HttpContext.User.IsInRole(Constants.FranchHolderRoleSouth))
            {
                id = 4;
            }
            else if (HttpContext.User.IsInRole(Constants.FranchHolderRoleWest))
            {
                id = 5;
            }
            return id;
        }


        public async Task<IActionResult> Inventory()
        {
            _storeID = GetStoreID();

            var query = _context.StoreInventory.Include(x => x.Product).Where(x => x.StoreID == _storeID).Select(x => x);

            query = query.OrderBy(x => x.Product.ProductID);

            // Passing a List of models object to the View.
            return View(await query.ToListAsync());
        }


        public async Task<IActionResult> AddStockRequest(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            //bind the Product object
            var storeInventory = await _context.StoreInventory.SingleOrDefaultAsync(m => m.ProductID == id && m.StoreID == GetStoreID());
            var product = await _context.Product.SingleOrDefaultAsync(m => m.ProductID == storeInventory.ProductID);
            storeInventory.Product = product;
            if (storeInventory == null)
            {
                return NotFound();
            }
            return View(storeInventory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStockRequest(int id, [Bind("ProductID,Product,StockLevel")] StoreInventory storeInventory)
        {
            if (id != storeInventory.ProductID)
            {
                return NotFound();
            }
            if (storeInventory.StockLevel <= 0)
            {
                return NotFound("Invalid Quantity!");
            }
            StockRequest newRequest = new StockRequest();
            newRequest.StoreID = GetStoreID();
            newRequest.ProductID = storeInventory.ProductID;
            newRequest.Quantity = storeInventory.StockLevel;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(newRequest);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(storeInventory.ProductID))
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
            return View(storeInventory);
        }
        private bool ProductExists(int id)
        {
            return _context.Product.Any(e => e.ProductID == id);
        }

        public async Task<IActionResult> NotInventory()
        {
            _storeID = GetStoreID();

            var query = _context.StoreInventory.Include(x => x.Product).Where(x => x.StoreID == _storeID).Select(x => x);
            var products = _context.Product.ToList();

            foreach (var q in query)
            {
                if (products.Contains(q.Product))
                {
                    products.Remove(q.Product);
                }
            }
            query = query.OrderBy(x => x.Product.ProductID);

            return View(products);
        }

        public async Task<IActionResult> NoStockRequest(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var storeInventory = await _context.OwnerInventory.SingleOrDefaultAsync(m => m.ProductID == id);
            var product = await _context.Product.SingleOrDefaultAsync(m => m.ProductID == storeInventory.ProductID);
            storeInventory.Product = product;
            if (storeInventory == null)
            {
                return NotFound();
            }
            return View(storeInventory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNoStockRequest(int productID, [Bind("ProductID,Product,StockLevel")] OwnerInventory ownerInventory)
        {
            if (productID != ownerInventory.ProductID)
            {
                return NotFound();
            }

            _storeID = GetStoreID();

            StockRequest newRequest = new StockRequest();
            newRequest.StoreID = _storeID;
            newRequest.ProductID = ownerInventory.ProductID;
            newRequest.Quantity = ownerInventory.StockLevel;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(newRequest);
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
            return NotFound();
        }
        

    }
}
