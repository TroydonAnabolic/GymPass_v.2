using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GymPass.Models
{
    // TODO: Change name to something that means gym user records out and inside of gym
    public class UsersOutOfGymDetails
    {
        public int UsersOutOfGymDetailsID { get; set; }
        public int FacilityID { get; set; }
        public DateTime EstimatedTimeToCheck { get; set; } 
        public string UniqueEntryID { get; set; } = string.Empty;// used to identify which entry to remove when the user leaves

        public Facility Facility { get; set; }
    }
}
