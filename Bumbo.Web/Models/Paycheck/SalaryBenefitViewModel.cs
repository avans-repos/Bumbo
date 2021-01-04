﻿using System.Collections.Generic;
using Bumbo.Data.Models;
using Bumbo.Logic.PayCheck;

namespace Bumbo.Web.Models.Paycheck
{
    public class SalaryBenefitViewModel : WorkedShift
    {
        public Dictionary<User, PayCheck> PayChecks = new Dictionary<User, PayCheck>();
        public double ExtraTime { get; set; }
    }
}