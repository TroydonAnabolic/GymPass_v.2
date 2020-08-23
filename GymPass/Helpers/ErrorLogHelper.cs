using GymPass.Data;
using GymPass.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GymPass.Helpers
{
    public class ErrorLogHelper
    {
        private readonly FacilityContext _facilityContext;

        public ErrorLogHelper(FacilityContext facilityContext)
        {
            _facilityContext = facilityContext;
        }

        [BindProperty]
        public Error Errors { get; set; }
        public async Task SaveErrorPageToDB(Exception e)
        {
            Errors.ErrorMessage = string.Empty;
            var errorMessage = new Error();
            var entry = _facilityContext.Add(errorMessage);
            Errors.ErrorMessage = $"ERROR! An error occured: {e.Message}. Time of ERROR = {DateTime.Now}.";
            entry.CurrentValues.SetValues(Errors);
            await _facilityContext.SaveChangesAsync();
        }
    }
}
