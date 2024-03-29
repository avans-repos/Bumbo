﻿using System.Threading.Tasks;
using Bumbo.Data;
using Bumbo.Data.Models;
using Bumbo.Data.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Bumbo.Web.Controllers
{
    [Authorize(Policy = "SuperUser")]
    [Route("{controller}/{action=Index}")]
    public class ForecastStandardController : Controller
    {
        private readonly RepositoryWrapper _wrapper;

        public ForecastStandardController(RepositoryWrapper wrapper)
        {
            _wrapper = wrapper;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _wrapper.ForecastStandard.GetAll());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Activity,Value")] ForecastStandard forecastStandard)
        {
            if (!ModelState.IsValid)
            {
                return View(forecastStandard);
            }
            await _wrapper.ForecastStandard.Add(forecastStandard);
            return RedirectToAction(nameof(Index));
        }

        [Route("{activity}")]
        public async Task<IActionResult> Edit(ForecastActivity activity)
        {
            var forecastStandard = await _wrapper.ForecastStandard.Get(fs => fs.Activity == activity);
            if (forecastStandard == null)
            {
                return NotFound();
            }
            return View(forecastStandard);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("{activity}")]
        public async Task<IActionResult> Edit(ForecastActivity activity, [Bind("Activity,Value")] ForecastStandard forecastStandard)
        {
            if (activity != forecastStandard.Activity)
            {
                return NotFound();
            }
            if (!ModelState.IsValid)
            {
                return View(forecastStandard);
            }

            try
            {
                await _wrapper.ForecastStandard.Update(forecastStandard);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await _wrapper.ForecastStandard.Get(
                fs => fs.Activity == forecastStandard.Activity,
                fs => fs.Value == forecastStandard.Value) == null)
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
