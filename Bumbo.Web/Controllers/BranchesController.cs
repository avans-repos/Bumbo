﻿using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bumbo.Data;
using Bumbo.Data.Models;
using Bumbo.Web.Models.Branches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Bumbo.Web.Controllers
{
    [Authorize(Policy = "BranchEmployee")]
    [Route("Branches/{branchId?}/{action=Index}")]
    public class BranchesController : Controller
    {
        private readonly RepositoryWrapper _wrapper;
        private readonly HttpContext _httpContext;

        public BranchesController(RepositoryWrapper wrapper, IHttpContextAccessor httpContextAccessor)
        {
            _wrapper = wrapper;
            _httpContext = httpContextAccessor.HttpContext;
        }

        [Authorize(Policy = "SuperUser")]
        public async Task<IActionResult> Index()
        {
            return View(await _wrapper.Branch.GetAll());
        }

        public async Task<IActionResult> Details(int? branchId)
        {
            if (branchId == null)
            {
                return NotFound();
            }

            var branch = await _wrapper.Branch.Get(b => b.Id == branchId);
            if (branch == null)
            {
                return NotFound();
            }

            var managersForBranch = _wrapper.BranchManager.GetAll(bm => bm.BranchId == branchId).Result
                .Select(bm => bm.User).ToList();

            return View(new DetailsViewModel
            {
                CurrentUserId = GetCurrentUserId(),
                Branch = branch,
                Managers = managersForBranch
            });
        }

        [Authorize(Policy = "SuperUser")]
        public IActionResult Create()
        {
            return View();
        }
        
        [Authorize(Policy = "SuperUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,ZipCode,HouseNumber")] Branch branch)
        {
            if (!ModelState.IsValid) return View(branch);
            await _wrapper.Branch.Add(branch);
            return RedirectToAction("Index");
        }

        [Authorize(Policy = "BranchManager")]
        public async Task<IActionResult> Edit(int? branchId)
        {
            if (branchId == null) return NotFound();

            var branch = await _wrapper.Branch.Get(b => b.Id == branchId);
            if (branch == null) return NotFound();
            return View(branch);
        }
        
        [Authorize(Policy = "BranchManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int branchId, [Bind("Name,ZipCode,HouseNumber,Id")] Branch branch)
        {
            if (branchId != branch.Id) return NotFound();
            if (!ModelState.IsValid) return View(branch);
            
            try
            {
                branch = await _wrapper.Branch.Update(branch);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await _wrapper.Branch.Get(b => b.Id == branch.Id) == null) return NotFound();
                throw;
            }
            return RedirectToAction(
                "Details",
                new {
                    branchId = branch.Id
                });
        }

        [Authorize(Policy = "SuperUser")]
        public async Task<IActionResult> Delete(int? branchId)
        {
            if (branchId == null) return NotFound();

            var branch = await _wrapper.Branch.Get(b => b.Id == branchId);
            if (branch == null) return NotFound();

            return View(branch);
        }

        [Authorize(Policy = "SuperUser")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int branchId)
        {
            var branch = await _wrapper.Branch.Get(b => b.Id == branchId);
            await _wrapper.Branch.Remove(branch);
            return RedirectToAction("Index");
        }

        [Authorize(Policy = "BranchManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveManager(int userId, int branchId)
        {
            // Manager can't remove itself
            if (GetCurrentUserId() == userId) return RedirectToAction("Details");

            var branchManager = await _wrapper.BranchManager.Get(
                bm => bm.BranchId == branchId,
                bm => bm.UserId == userId
            );
            await _wrapper.BranchManager.Remove(branchManager);

            return RedirectToAction("Details");
        }

        [Authorize(Policy = "BranchManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddManager(string userEmail, int branchId)
        {
            var user = await _wrapper.User.Get(u => u.Email == userEmail);
            // Check if manager already exists
            if (await _wrapper.BranchManager.Get(
                bm => bm.BranchId == branchId,
                bm => bm.UserId == user.Id
            ) != null) return RedirectToAction("Details");
            
            await _wrapper.BranchManager.Add(new BranchManager
            {
                BranchId = branchId,
                UserId = user.Id
            });

            return RedirectToAction("Details");
        }

        private int GetCurrentUserId()
        {
            return int.Parse(_httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }
}
