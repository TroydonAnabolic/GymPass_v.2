using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace GymPass.Helpers
{
    public class SelectListHelper
    {
        // selects training duration
        public static IEnumerable<SelectListItem> GetTrainingTimes()
        {
            IList<SelectListItem> items = new List<SelectListItem>
            {
                new SelectListItem{Text = "0 minutes", Value = TimeSpan.FromMinutes(0).ToString()},
                new SelectListItem{Text = "20 minutes", Value = TimeSpan.FromMinutes(20).ToString()},
                new SelectListItem{Text = "40 minutes", Value = TimeSpan.FromMinutes(40).ToString()},
                new SelectListItem{Text = "1 hour", Value = TimeSpan.FromHours(1).ToString()},
                new SelectListItem{Text = "1 hour 30 minutes", Value = TimeSpan.FromHours(1.5).ToString()},
                new SelectListItem{Text = "More than 2 hours", Value = "More than 2 hours"}
            };
            return items;
        }
    }
}
