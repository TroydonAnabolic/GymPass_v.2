using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GymPass.Models
{
    public class Facility
    {
        public int FacilityID { get; set; }
        public ICollection<UsersInGymDetail> UsersInGymDetails { get; set; }
        public ICollection<UsersOutOfGymDetails> UsersOutOfGymDetails { get; set; }
        public string FacilityName { get; set; } // TODO: user javascript to split names on caps except first letter, then add a space to replace
        public int? NumberOfClientsInGym { get; set; }
        public int? NumberOfClientsUsingWeightRoom { get; set; }
        public int? NumberOfClientsUsingCardioRoom { get; set; }
        public int? NumberOfClientsUsingStretchRoom{ get; set; }
        public bool IsOpenDoorRequested { get; set; } 
        public bool DoorOpened { get; set; } 

        public TimeSpan DoorCloseTimer { get; set; } = TimeSpan.FromSeconds(5);
        [Display(Name = "Workout Duration")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:hh\\:mm}")]
        [RegularExpression(@"((([0-1][0-9])|(2[0-3]))(:[0-5][0-9])(:[0-5][0-9])?)", ErrorMessage = "Time must be between 00:00 to 23:59")]
        public TimeSpan UserTrainingDuration { get; set; }
        public TimeSpan TotalTrainingDuration { get; set; }
        [Display(Name = "Weights Room")]
        public bool WillUseWeightsRoom { get; set; }
        [Display(Name = "Cardio Room")]
        public bool WillUseCardioRoom { get; set; }
        [Display(Name = "Stretch Room")]
        public bool WillUseStretchRoom { get; set; }
        public bool IsCameraScanSuccessful { get; set; }
        public bool IsWithin10m { get; set; }
        public string Latitude { get; set; } = "-34.006388";
        public string Longitude { get; set; } = "150.858975";
    }
}
