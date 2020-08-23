using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GymPass.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int UsernameChangeLimit { get; set; } = 10;
        public int DefaultGym { get; set; }


        // Facility Details
        public bool AccessGrantedToFacility { get; set; }
        public bool IsCameraScanSuccessful { get; set; }
        public bool IsWithin10m { get; set; }
        public bool IsInsideGym { get; set; }
        public bool OpenDoorRequest { get; set; }
        public bool ExitGymRequest { get; set; }
        public DateTime TimeAccessDenied { get; set; }
        public DateTime TimeAccessGranted { get; set; }
        public TimeSpan IntendedTrainingDuration { get; set; }
        public bool WillUseWeightsRoom { get; set; } 
        public bool WillUseCardioRoom { get; set; }
        public bool WillUseStretchRoom { get; set; } 
        public bool HasLoggedWorkoutToday { get; set; }
        public DateTime TimeLoggedWorkout { get;  set; }
//        public bool SkipWorkoutLogging { get; set; }

        [Display(Name = "User Image")]
        public byte[] UserImage { get; set; }
        public bool isVerifiedUser { get; set; } // TODO: admin user can view all requests, and save all user registrations - once approved, users can login.
        public string TestLat { get; set; }
        public string TestLong { get; set; }

    }
}
