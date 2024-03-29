﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Bumbo.Data;
using Bumbo.Data.Models;
using Bumbo.Data.Models.Enums;
using Bumbo.Logic.Utils;
using Bumbo.Web.Models.Furlough;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
namespace Bumbo.Web.Controllers
{
    [Authorize]
    [Route("Branches/{branchId}/{controller}/{action=Index}")]
    public class FurloughController : Controller
    {

        private readonly IStringLocalizer<FurloughController> _localizer;
        private readonly UserManager<User> _userManager;
        private readonly RepositoryWrapper _wrapper;

        public FurloughController(RepositoryWrapper wrapper, UserManager<User> userManager, IStringLocalizer<FurloughController> localizer)
        {
            _wrapper = wrapper;
            _userManager = userManager;
            _localizer = localizer;
        }

        [Authorize(Policy = "BranchEmployee")]
        public async Task<IActionResult> Index()
        {
            var furloughs = await _wrapper.Furlough.GetAll(f => f.UserId == int.Parse(_userManager.GetUserId(User)) && f.EndDate >= DateTime.Now);

            return View("Employee/Index", new FurloughViewModel
            {
                Furloughs = furloughs
            });
        }
       
        [HttpPost]
        [Authorize(Policy = "BranchEmployee")]
        public async Task<IActionResult> Create(int branchId, int? id, FurloughViewModel.InputModel furloughModel)
        {
            var userId = int.Parse(_userManager.GetUserId(User));

            if (!ModelState.IsValid)
            {
                TempData["AlertMessage"] = $"danger:{_localizer["InvalidData"]}";
                return RedirectToAction(nameof(Index));
            }

            if (!furloughModel.IsAllDay)
            {
                furloughModel.StartDate += furloughModel.StartTime.Value;
                furloughModel.EndDate += furloughModel.EndTime.Value;
            }
            else
            {
                furloughModel.EndDate += new TimeSpan(23, 59, 59);
            }

            var shifts = await _wrapper.Shift.GetAll(shift => shift.UserId == userId && shift.Date > furloughModel.StartDate && shift.Date < furloughModel.EndDate);

            if (shifts.Any())
            {
                TempData["AlertMessage"] = $"danger:{_localizer["NotAllowed"]}";
            }
            else
            {
                var presentFurlough = await _wrapper.Furlough.Get(f => f.Id == id);

                if (presentFurlough == null)
                {
                    var furlough = new Furlough
                    {
                        UserId = userId,
                        BranchId = branchId,
                        Description = furloughModel.Description,
                        StartDate = furloughModel.StartDate,
                        EndDate = furloughModel.EndDate,
                        IsAllDay = furloughModel.IsAllDay,
                        Status = FurloughStatus.REQUESTED
                    };

                    if (await _wrapper.Furlough.Add(furlough) != null)
                    {
                        TempData["AlertMessage"] = $"success:{_localizer["FurloughSaved"]}";
                    }
                }
                else
                {
                    presentFurlough.Description = furloughModel.Description;
                    presentFurlough.StartDate = furloughModel.StartDate;
                    presentFurlough.EndDate = furloughModel.EndDate;
                    presentFurlough.IsAllDay = furloughModel.IsAllDay;

                    if (await _wrapper.Furlough.Update(presentFurlough) != null)
                    {
                        TempData["AlertMessage"] = $"success:{_localizer["FurloughUpdated"]}";
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "BranchEmployee")]
        public async Task<IActionResult> Delete(int id)
        {
            TempData["AlertMessage"] = $"danger:{_localizer["SomethingWentWrong"]}";

            var userId = int.Parse(_userManager.GetUserId(User));
            if (await _wrapper.Furlough.Remove(f => (f.Id == id) & (f.UserId == userId)) != null)
            {
                TempData["AlertMessage"] = $"success:{_localizer["FurloughDeleted"]}";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "Manager")]
        public async Task<IActionResult> Overview(int branchId)
        {
            var furloughs = await _wrapper.Furlough.GetAll(f => f.EndDate >= DateTime.Now && f.Status == FurloughStatus.REQUESTED && f.BranchId == branchId);
            var users = furloughs.Select(f => f.User).Distinct().ToList();

            return View("Manager/Index", new ManagerFurloughViewModel
            {
                UserFurloughs = users.ToDictionary(user => new ManagerFurloughViewModel.User
                {
                    Id = user.Id, Name = UserUtil.GetFullName(user)
                }, user => furloughs
                    .Where(f => f.User == user)
                    .Select(f => new ManagerFurloughViewModel.Furlough
                    {
                        Id = f.Id,
                        UserId = f.UserId,
                        BranchId = branchId,
                        Description = f.Description,
                        StartDate = f.StartDate,
                        EndDate = f.EndDate,
                        Status = f.Status,
                        IsAllDay = f.IsAllDay
                    })
                    .ToList())
            });
        }

        [HttpPost]
        [Authorize(Policy = "Manager")]
        public async Task<IActionResult> UpdateFurloughStatus(int id, FurloughStatus status)
        {
            TempData["AlertMessage"] = $"danger:{_localizer["FurloughNotUpdated"]}";

            if (!ModelState.IsValid)
            {
                TempData["AlertMessage"] = $"danger:{_localizer["InvalidData"]}";
                return RedirectToAction(nameof(Overview));
            }

            var furlough = await _wrapper.Furlough.Get(f => f.Id == id);

            furlough.Status = status;

            if (await _wrapper.Furlough.Update(furlough) != null)
            {
                TempData["AlertMessage"] = $"success:{_localizer["FurloughUpdated"]}";
            }

            return RedirectToAction(nameof(Overview));
        }
    }
}
