﻿using Bumbo.Data;
using Bumbo.Data.Models;
using Bumbo.Web.Models.Schedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bumbo.Web.Controllers
{
    //[Authorize(Policy = "BranchEmployee")]
    [Route("{controller}/{action=Index}")]
    public class PersonalScheduleController : Controller
    {
        private readonly RepositoryWrapper _wrapper;
        private readonly IStringLocalizer<PersonalScheduleController> _localizer;
        private readonly UserManager<User> _userManager;

        public PersonalScheduleController(RepositoryWrapper wrapper, IStringLocalizer<PersonalScheduleController> localizer, UserManager<User> userManager)
        {
            _wrapper = wrapper;
            _localizer = localizer;
            _userManager = userManager;
        }
        
        public IActionResult Index()
        {
            return View(new EventViewModel());
        }

        [HttpGet]
        public async Task<JsonResult> GetCalendarEvents()
        {
            var viewModel = new EventViewModel();
            var events = new List<EventViewModel>();

            var userId = int.Parse(_userManager.GetUserId(User));

            var shifts = await _wrapper.Shift.GetAll(
                       shift => shift.UserId == userId
                       //shift => shift.Date >= start,
                       //shift => shift.Date < end.AddDays(7)
                   );

            foreach (var i in shifts)
            {
                events.Add(new EventViewModel()
                {
                    id = i.Id,
                    title = i.Department.ToString(),
                    start = $"{i.Date.ToString("yyyy-MM-dd")}T{i.StartTime}",
                    end = $"{ i.Date.ToString("yyyy-MM-dd") }T{ i.EndTime }",
                    allDay = false
                });
            }

            return Json(events.ToArray());
        }
    }
}
