﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Bumbo.Logic.EmployeeRules;

namespace Bumbo.Web.Models.Paycheck
{
    public class DetailsViewModel
    {
        public Dictionary<int, List<Shift>> WeekShifts { get; set; }

        public InputModel Input { get; set; }

        public readonly DayOfWeek[] DaysOfWeek =
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday
        };

        public class Shift
        {
            public int ShiftId { get; set; }

            [Display(Name = "Start Time")]
            [DisplayFormat(DataFormatString = "{0:hh\\:mm}")]
            public TimeSpan StartTime { get; set; }

            [Display(Name = "End Time")]
            [DisplayFormat(DataFormatString = "{0:hh\\:mm}")]
            public TimeSpan EndTime { get; set; }

            [Display(Name = "Day of Week")]
            public DayOfWeek Day { get; set; }

            [Display(Name = "Break Time")]
            [DisplayFormat(DataFormatString = "{0:hh\\:mm}")]
            public TimeSpan BreakTime => BreakDuration.GetDuration(TotalTime);

            [Display(Name = "Work Time")]
            [DisplayFormat(DataFormatString = "{0:hh\\:mm}")]
            public TimeSpan WorkTime => TotalTime.Subtract(BreakTime);

            [Display(Name = "Total Time")]
            [DisplayFormat(DataFormatString = "{0:hh\\:mm}")]
            public TimeSpan TotalTime => EndTime.Subtract(StartTime);

            [Display(Name = "Difference")]
            [DisplayFormat(DataFormatString = "{0:hh\\:mm}")]
            public TimeSpan Difference { get; set; }
        }

        public class InputModel
        {
            [Required]
            public int ShiftId { get; set; }
            public int UserId { get; set; }
            public int Year { get; set; }
            public int Month { get; set; }

            [Display(Name = "Start Time")]
            [DataType(DataType.Time)]
            [DisplayFormat(DataFormatString = "{0:hh\\:mm}")]
            [Required]
            public TimeSpan StartTime { get; set; }

            [Display(Name = "End Time")]
            [DataType(DataType.Time)]
            [DisplayFormat(DataFormatString = "{0:hh\\:mm}")]
            [Required]
            public TimeSpan EndTime { get; set; }
        }
    }
}