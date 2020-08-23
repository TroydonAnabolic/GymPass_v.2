using GymPass.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GymPass.Data
{
    public static class DbInitializer
    {
        public static void Initialize(FacilityContext facilityContext)
        {
            // Look for any students.
            if (facilityContext.Facilities.Any())
            {
                return;   // DB has been seeded
            }

            // if none then seed
            var facilities = new Facility[]
           {
                new Facility{FacilityName="Super Saiyan Gym",NumberOfClientsInGym=0,NumberOfClientsUsingCardioRoom =0,NumberOfClientsUsingStretchRoom=0,NumberOfClientsUsingWeightRoom=0,
                IsOpenDoorRequested=false,DoorOpened=false,DoorCloseTimer=default(TimeSpan),UserTrainingDuration=default(TimeSpan),TotalTrainingDuration=default(TimeSpan),WillUseCardioRoom=false,
                WillUseStretchRoom=false,WillUseWeightsRoom=false,IsCameraScanSuccessful=false,IsWithin10m=false,Latitude=string.Empty,Longitude=string.Empty,},
           };

            facilityContext.Facilities.AddRange(facilities);
            facilityContext.SaveChanges();

        }
    }
}
