using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BangazonAuth.Models;
using BangazonAuth.Data;
using BangazonAuth.Models.ProductViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BangazonAuth.Controllers
{
    public class ProductsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private ApplicationDbContext context;
        public ProductsController(UserManager<ApplicationUser> userManager, ApplicationDbContext ctx)
        {
            _userManager = userManager;
            context = ctx;
        }

        // This task retrieves the currently authenticated user
        private Task<ApplicationUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        public async Task<IActionResult> Index()
        {
            // Create new instance of the view model
            ProductListViewModel model = new ProductListViewModel();

            // Set the properties of the view model
            model.Products = await context.Product.ToListAsync(); 
            return View(model);
        }

        public async Task<IActionResult> Detail([FromRoute]int? id)
        {
            // If no id was in the route, return 404
            if (id == null)
            {
                return NotFound();
            }

            // Create new instance of view model
            ProductDetail model = new ProductDetail();

            // Set the `Product` property of the view model
            model.Product = await context.Product
                    .Include(prod => prod.User)
                    .SingleOrDefaultAsync(prod => prod.ProductId == id);

            // If product not found, return 404
            if (model.Product == null)
            {
                return NotFound();
            }

            return View(model); 
        }

        [HttpGet]
        [Authorize]
        public IActionResult Create()
        {
            ProductCreateViewModel model = new ProductCreateViewModel(context);

            return View(model); 
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            // Remove the user from the model validation because it is
            // not information posted in the form
            ModelState.Remove("product.User");

            if (ModelState.IsValid)
            {
                /*
                    If all other properties validation, then grab the 
                    currently authenticated user and assign it to the 
                    product before adding it to the db context
                */
                var user = await GetCurrentUserAsync();
                var userId = user?.Id;
                product.User = user;

                context.Add(product);

                await context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ProductCreateViewModel model = new ProductCreateViewModel(context);
            return View(model);
        }

        public async Task<IActionResult> Types()
        {
            var model = new ProductTypesViewModel();

            // Get line items grouped by product id, including count
            var counter = from product in context.Product
                    group product by product.ProductTypeId into grouped
                    select new { grouped.Key, myCount = grouped.Count() };

            // Build list of Product instances for display in view
            model.ProductTypes = await (from type in context.ProductType
                    join a in counter on type.ProductTypeId equals a.Key 
                    select new ProductType {
                        ProductTypeId = type.ProductTypeId,
                        Label = type.Label, 
                        Quantity = a.myCount 
                    }).ToListAsync();

            return View(model);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}