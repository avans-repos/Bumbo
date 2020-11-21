using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bumbo.Data;
using Bumbo.Data.Models;
using Bumbo.Data.Models.Enums;
using Bumbo.Data.Repositories;
using Bumbo.Logic.EmployeeRules;
using Bumbo.Logic.Utils;
using Bumbo.Web.Models.Schedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bumbo.Web.Controllers
{
    [Authorize(Policy = "BranchManager")]
    [Route("Branches/{branchId}/{controller}/{action=Index}")]
    public class ScheduleController : Controller
    {
        private readonly RepositoryWrapper _wrapper;

        public ScheduleController(RepositoryWrapper wrapper)
        {
            _wrapper = wrapper;
        }

        [Route("{year}/{week}/{department}")]
        public async Task<IActionResult> Department(int branchId, int year, int week, Department department)
        {
            var branch = await _wrapper.Branch.Get(branch1 => branch1.Id == branchId);

            if (branch == null) return NotFound();

            if (TempData["alertMessage"] != null)
            {
                ViewData["AlertMessage"] = TempData["alertMessage"];
            }

            try
            {
                var users = await _wrapper.User.GetUsersAndShifts(branch, year, week, department);

                return View(new DepartmentViewModel
                {
                    Year = year,
                    Week = week,

                    Department = department,

                    Branch = branch,
                    
                    ScheduleApproved = branch.WeekSchedules
                        .Where(schedule => schedule.Year == year)
                        .Where(schedule => schedule.Week == week)
                        .FirstOrDefault(schedule => schedule.Department == department)?.Confirmed ?? false,

                    EmployeeShifts = users.Select(user => new DepartmentViewModel.EmployeeShift
                    {
                        UserId = user.Id,
                        Name = UserUtil.GetFullName(user),
                        Contract = user.Contracts.FirstOrDefault()?.Function ?? "",

                        MaxHours = WorkingHours.MaxHoursPerWeek(user, year, week),

                        Scale = user.Contracts.FirstOrDefault()?.Scale ?? 0,

                        Shifts = user.Shifts.Select(shift =>
                        {
                            var notifications = WorkingHours.ValidateWeek(user, year, week);

                            return new DepartmentViewModel.Shift
                            {
                                Id = shift.Id,
                                Date = shift.Date,
                                StartTime = shift.StartTime,
                                EndTime = shift.EndTime,
                                Notifications = notifications.First(pair => pair.Key.Id == shift.Id).Value
                            };
                        }).ToList()
                    }).ToList(),

                    InputShift = new DepartmentViewModel.InputShiftModel
                    {
                        Year = year,
                        Week = week,
                        Department = department
                    },

                    InputCopyWeek = new DepartmentViewModel.InputCopyWeekModel
                    {
                        Year = year,
                        Week = week,
                        Department = department
                    },

                    InputApproveSchedule = new DepartmentViewModel.InputApproveScheduleModel
                    {
                        Year = year,
                        Week = week,
                        Department = department
                    }
                });
            }
            catch (ArgumentOutOfRangeException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveShift(int branchId, DepartmentViewModel.InputShiftModel shiftModel)
        {
            var branch = await _wrapper.Branch.Get(branch1 => branch1.Id == branchId);

            if (branch == null) return NotFound();

            var alertMessage = "Danger:De dienst kon niet worden opgeslagen.";

            if (ModelState.IsValid)
            {
                var shift = await _wrapper.Shift.Get(
                    shift1 => shift1.BranchId == branch.Id,
                    shift1 => shift1.Id == shiftModel.ShiftId);

                bool success;

                if (shift == null)
                {
                    shift = new Shift
                    {
                        Department = shiftModel.Department,
                        BranchId = branch.Id,
                        UserId = shiftModel.UserId,
                        Date = shiftModel.Date,
                        StartTime = shiftModel.StartTime,
                        EndTime = shiftModel.EndTime
                    };

                    success = await _wrapper.Shift.Add(shift) != null;
                }
                else
                {
                    shift.StartTime = shiftModel.StartTime;
                    shift.EndTime = shiftModel.EndTime;

                    success = await _wrapper.Shift.Update(shift) != null;
                }

                if (success)
                {
                    alertMessage = "Success:De dienst is succesvol opgeslagen.";
                }
            }

            TempData["alertMessage"] = alertMessage;

            return RedirectToAction(nameof(Department), new
            {
                branchId,
                year = shiftModel.Year,
                week = shiftModel.Week,
                department = shiftModel.Department,
            });
        }

        [HttpPost]
        public async Task<IActionResult> CopySchedule(int branchId, DepartmentViewModel.InputCopyWeekModel copyWeekModel)
        {
            var branch = await _wrapper.Branch.Get(branch1 => branch1.Id == branchId);

            if (branch == null) return NotFound();

            TempData["alertMessage"] = "Danger:Het rooster kon niet worden gekopieerd.";

            if (ModelState.IsValid)
            {
                try
                {
                    var startDateTarget = ISOWeek.ToDateTime(copyWeekModel.TargetYear, copyWeekModel.TargetWeek, DayOfWeek.Monday);

                    var targetShifts = await _wrapper.Shift.GetAll(
                        shift => shift.BranchId == branch.Id,
                        shift => shift.Department == copyWeekModel.Department,
                        shift => shift.Date >= startDateTarget,
                        shift => shift.Date < startDateTarget.AddDays(7)
                    );

                    if (!targetShifts.Any())
                    {
                        var startDate = ISOWeek.ToDateTime(copyWeekModel.Year, copyWeekModel.Week, DayOfWeek.Monday);
                        var shifts = await _wrapper.Shift.GetAll(
                            shift => shift.BranchId == branch.Id,
                            shift => shift.Department == copyWeekModel.Department,
                            shift => shift.Date >= startDate,
                            shift => shift.Date < startDate.AddDays(7)
                        );

                        var newShifts = shifts.Select(shift => new Shift
                        {
                            BranchId = shift.BranchId,
                            Department = shift.Department,
                            UserId = shift.UserId,
                            Date = ISOWeek.ToDateTime(copyWeekModel.TargetYear, copyWeekModel.TargetWeek, shift.Date.DayOfWeek),
                            StartTime = shift.StartTime,
                            EndTime = shift.EndTime
                        }).ToArray();

                        if (await _wrapper.Shift.AddRange(newShifts) != null)
                        {
                            TempData["alertMessage"] = $"Success:Het rooster is succesvol gekopieerd naar week {copyWeekModel.TargetWeek} van {copyWeekModel.TargetYear}.";

                            return RedirectToAction(nameof(Department), new
                            {
                                branchId,
                                year = copyWeekModel.TargetYear,
                                week = copyWeekModel.TargetWeek,
                                department = copyWeekModel.Department
                            });
                        }
                    }
                    else
                    {
                        TempData["alertMessage"] = "Danger:Het rooster in de gekozen week bevat al diensten.";
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    TempData["alertMessage"] = "Danger:De gekozen week bestaat niet.";
                }
            }

            return RedirectToAction(nameof(Department), new
            {
                branchId,
                year = copyWeekModel.Year,
                week = copyWeekModel.Week,
                department = copyWeekModel.Department
            });
        }

        [HttpPost]
        public async Task<IActionResult> ApproveSchedule(int branchId, DepartmentViewModel.InputApproveScheduleModel approveScheduleModel)
        {
            var branch = await _wrapper.Branch.Get(branch1 => branch1.Id == branchId);

            if (branch == null) return NotFound();

            TempData["alertMessage"] = "Danger:Het rooster kon niet worden goedgekeurd.";

            if (ModelState.IsValid)
            {
                try
                {
                    var startDate = ISOWeek.ToDateTime(approveScheduleModel.Year, approveScheduleModel.Week, DayOfWeek.Monday);
                    var shifts = await _wrapper.Shift.GetAll(
                        shift => shift.BranchId == branch.Id,
                        shift => shift.Department == approveScheduleModel.Department,
                        shift => shift.Date >= startDate,
                        shift => shift.Date < startDate.AddDays(7)
                    );

                    if (shifts.Any())
                    {
                        var weekSchedule = new WeekSchedule
                        {
                            BranchId = branch.Id,
                            Year = approveScheduleModel.Year,
                            Week = approveScheduleModel.Week,
                            Department = approveScheduleModel.Department,
                            Confirmed = true
                        };

                        if (await _wrapper.WeekSchedule.Add(weekSchedule) != null)
                        {
                            TempData["alertMessage"] = "Success:Het rooster is succesvol goedgekeurd";
                        }
                    }
                    else
                    {
                        TempData["alertMessage"] = "Danger:Het rooster bevat geen diensten";
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    TempData["alertMessage"] = "Danger:De gekozen week bestaat niet.";
                }
            }

            return RedirectToAction(nameof(Department), new
            {
                branchId,
                year = approveScheduleModel.Year,
                week = approveScheduleModel.Week,
                department = approveScheduleModel.Department
            });
        }
    }
}