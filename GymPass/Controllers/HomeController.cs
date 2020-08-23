using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GymPass.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using GymPass.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.S3;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text;

namespace GymPass.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FacilityContext _facilityContext;
        IAmazonS3 S3Client { get; set; }
        IAmazonRekognition AmazonRekognition { get; set; }
        private const string bucket = "gym-user-bucket-i";

        // dependency injections for services
        public HomeController(
            UserManager<ApplicationUser> userManager,
            ILogger<HomeController> logger,
            FacilityContext facilityContext,
            IAmazonS3 s3Client,
            IAmazonRekognition amazonRekognition
            )
        {
            _userManager = userManager;
            _logger = logger;
            _facilityContext = facilityContext;
            S3Client = s3Client;
            AmazonRekognition = amazonRekognition;
        }

        [BindProperty]
        UsersInGymDetail UsersInGymDetail { get; set; }

        // GET: Home/Index/1
        // TODO: We begin using id = 1 for now, later will implement dynamically changing this ID number, if it is null then redirect to action choose gym
        [HttpGet]
       //[Route("Home/Index/10")] // TODO: Test
        [Authorize]
        public async Task<IActionResult> Index(int? id)
        {
            // Get the default gym for a user and set it to be the Id for the gym being edited
            var user = await _userManager.GetUserAsync(User);
            ViewBag.EstimatedNumberInGym = 0;
            ViewBag.IsOpenDoorRequested = false;
            ViewBag.DefaultLat = user.TestLat;
            ViewBag.DefaultLong = user.TestLong;

            // if user does not exist return not found, if id is null, send to login page
            // if user does not exist return not found, if id is null, send to login page
            if (user.Id == null) return NotFound();
            id = user.DefaultGym; // set the ID to be current user's default gym
            if (id == null) return RedirectToPage("/Identity/Account/Login");
            // if (!user.isVerifiedUser) return RedirectToPage("/Identity/Account/Login");

            var facility = await _facilityContext.Facilities.FindAsync(id);
            var facilityDetails = await _facilityContext.UsersInGymDetails.ToListAsync();
            UsersInGymDetail = await _facilityContext.UsersInGymDetails.Where(f => f.UniqueEntryID == user.Id).FirstOrDefaultAsync();

            if (facility == null) return NotFound(); // ensure facility exists

            ViewBag.HomeUrl = Request.GetDisplayUrl(); // set url for dynamic allocation

            // if there is a user in gym, retreive facial recognition results from the database
            if (UsersInGymDetail != null)
            {
                ViewBag.IsSmiling = _facilityContext.UsersInGymDetails.Where(o => o.UniqueEntryID == user.Id).FirstOrDefault().IsSmiling;
                ViewBag.Gender = _facilityContext.UsersInGymDetails.Where(o => o.UniqueEntryID == user.Id).FirstOrDefault().Gender;
                ViewBag.AgeRangeLow = _facilityContext.UsersInGymDetails.Where(o => o.UniqueEntryID == user.Id).FirstOrDefault().AgeRangeLow;
                ViewBag.AgeRangeHigh = _facilityContext.UsersInGymDetails.Where(o => o.UniqueEntryID == user.Id).FirstOrDefault().AgeRangeHigh;
            }

            // Calculations for estimated time
            // get estimated time to check submitted to the db for the user submitting
            DateTime estimatedTimeToCheck = _facilityContext.UsersOutofGymDetails.Where(o => o.UniqueEntryID == user.Id).FirstOrDefault().EstimatedTimeToCheck;
            DateTime estimatedExitTimeCurrentUser = DateTime.Now;

            // default to false for access
            ViewBag.AccessGrantedToFacility = false;
            // door open status depends on database value
            ViewBag.DoorOpened = facility.DoorOpened;

            // Decide to increase or decrease the estimated numbers in gym
            // if there are entries get the estimated exit time
            estimatedExitTimeCurrentUser = GetEstimatedNumberOfGymUsers(facilityDetails, estimatedTimeToCheck, estimatedExitTimeCurrentUser);

            // access denied message is normally true
            // if time since the date where user was denied, is more than 5 seconds, then access denied msg received is not received so present the access denial
            ViewBag.AccessDeniedMsgRecieved = true;
            if (DateTime.Now <= (user.TimeAccessDenied.AddSeconds(10)))
                ViewBag.AccessDeniedMsgRecieved = false;

            // TODO: Logic for conditonally displaying if current time after last workout logged then send to log workout, and inside gym
            //if (DateTime.Now > user.TimeLoggedWorkout && user.IsInsideGym)
            //    return RedirectToAction("LogWorkout", "Facilities", new { id = user.DefaultGym });

            return View(facility);
        }

        private DateTime GetEstimatedNumberOfGymUsers(List<UsersInGymDetail> facilityDetails, DateTime estimatedTimeToCheck, DateTime estimatedExitTimeCurrentUser)
        {
            DateTime defaultValue =  default(DateTime);

            // if there are people in the gym
            if (facilityDetails.Count > 0)
            {
                int i = 0;
                foreach (var userInGym in facilityDetails)
                {
                    // get all the logged in users and assign est training time for each one
                    estimatedExitTimeCurrentUser = facilityDetails[i].TimeAccessGranted.AddMinutes(facilityDetails[i].EstimatedTrainingTime.TotalMinutes); // appears if user is not in the gym he cannot check, need to est exit time for all users

                    // if selected time has a lesser value, training has not finished, so we add to the count of estimated users in the facilities table
                    // somehow get the clicked value to replace this datetime.now. possibly use another action method
                    if (estimatedTimeToCheck == defaultValue) continue; // skip iteration if the user has never entered gym yet and has the 0 default time
                    if (estimatedTimeToCheck < estimatedExitTimeCurrentUser) //
                    {
                        // if the current user is still within his estimated training time, then add estimated number of gym users list
                        ViewBag.EstimatedNumberInGym++; //TODO: instead of viewbag, this will be data extracted fromt the db
                    }
                    // otherwise remove users from the gym, ensure not to go to negative
                    else if (estimatedTimeToCheck > estimatedExitTimeCurrentUser && ViewBag.EstimatedNumberInGym != 0)
                        ViewBag.EstimatedNumberInGym--;
                    i++;
                }
            }

            return estimatedExitTimeCurrentUser;
        }

        [HttpPost]
        [Route("Home/Index/{id?}", Name = "Auth")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(int id, [Bind("FacilityID,FacilityName,NumberOfClientsUsingWeightRoom,NumberOfClientsUsingCardioRoom," +
            "NumberOfClientsUsingStretchRoom,IsOpenDoorRequested,DoorOpened,DoorCloseTimer,IsCameraScanSuccessful, IsWithin10m")] Facility facilityView,
            [Bind("ErrorMessage")] Error Errors
            ) // 
        {
            // Get the default gym for a user and set it to be the Id for the gym being edited
            var user = await _userManager.GetUserAsync(User);

            if (user.Id == null) return NotFound();

            id = user.DefaultGym;

            // Variable facility is the database values, facilityView, binds data from the view to post to DB
            var facility = await _facilityContext.Facilities.FindAsync(id);
            var facilityDetails = await _facilityContext.UsersInGymDetails.ToListAsync();
            UsersInGymDetail currentFacilityDetail = new UsersInGymDetail();
            var currentFacilityDetailDb = await _facilityContext.UsersInGymDetails.Where(f => f.UniqueEntryID == user.Id).FirstOrDefaultAsync();
            var allGymUserRecords = await _facilityContext.UsersOutofGymDetails.Where(f => f.UniqueEntryID == user.Id).FirstOrDefaultAsync();
            bool enteredGym = false;

            if (id != facility.FacilityID || id != facilityView.FacilityID)
                return NotFound();

            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Index));

            if (ModelState.IsValid)
            {
                try
                { // maybe make is open door requested a user property
                  // if door open is requested from the view by clicking the button, then run the below logic to test if user is authorized and also apply crowdsensing functions
                    enteredGym = await DetermineEnterOrExitGym(facilityView, user, facility, facilityDetails, currentFacilityDetail, currentFacilityDetailDb, enteredGym, allGymUserRecords, Errors);
                }
                catch (DbUpdateConcurrencyException e)
                {
                    if (!FacilityExists(facility.FacilityID))
                        return NotFound();
                    else
                        _logger.LogInformation(e.Message);
                }

                // if the user has logged in successfully and not logged workout, then send to questionnaire TODO: for now, later will use AJAX to post questionaire here, and have option to later log in workout
                return (user.AccessGrantedToFacility) && (!user.HasLoggedWorkoutToday) ? RedirectToAction("LogWorkout", "Facilities", new { id = user.DefaultGym }) : RedirectToAction(nameof(Index));
            }
            return View(facility);
        }

        // Method that applies action based on whether user is entering or exiting the gym
        private async Task<bool> DetermineEnterOrExitGym(Facility facilityView, ApplicationUser user, Facility facility, List<UsersInGymDetail> facilityDetails, UsersInGymDetail currentFacilityDetail,
            UsersInGymDetail currentFacilityDetailDb, bool enteredGym, UsersOutOfGymDetails allGymUserRecords, Error Errors)
        {
            if (facilityView.IsOpenDoorRequested)
            {
                // perform facial recognition scan if not inside the gym
                if (!user.IsInsideGym) await FacialRecognitionScan(user, currentFacilityDetail, Errors);

                // Location scan (results from HERE API using javascript) is passed to this POST Action Method
                // when the checkbox's value is checked we assign user.IsWithin10m as true,
                //  satisfying the first requirement to gaining access to the facility
                if (facilityView.IsWithin10m)
                    user.IsWithin10m = true;

                // if camera scan and location check is true, and user is not in the gym, then we open the door, and access granted is true
                if (user.IsWithin10m && user.IsCameraScanSuccessful && !user.IsInsideGym)
                {
                    user.AccessGrantedToFacility = true;
                    ViewBag.AccessGrantedToFacility = true;
                }
                // if camera scan is not successful
                else if (!facilityView.IsCameraScanSuccessful && !user.IsWithin10m)
                {
                    user.AccessGrantedToFacility = false;
                }

                if (user.AccessGrantedToFacility)
                {
                    facility.DoorOpened = true;
                    // if the user is not in the gym, then say this user is not in the gym, and increase object values as required.
                    if (!user.IsInsideGym)
                    {
                        facility.NumberOfClientsInGym++;
                        user.IsInsideGym = true;
                        user.TimeAccessGranted = DateTime.Now;
                        currentFacilityDetail.TimeAccessGranted = DateTime.Now;
                        currentFacilityDetail.FirstName = user.FirstName;
                        currentFacilityDetail.UniqueEntryID = user.Id;
                        currentFacilityDetail.FacilityID = facility.FacilityID;
                        enteredGym = true;
                        // TODO: Use AJAX to async send to and from the client at the same time
                    }
                    // <--------------------------------- LEAVE GYM -----------------------------------------------------------
                    // if the user is already in the gym, when button is pushed then make reset all access to false, and decrement the number of ppl in the gym by 1
                    else if (user.IsInsideGym)
                    {
                        await LeaveGym(facilityView, user, facility, facilityDetails, currentFacilityDetailDb, Errors);
                    }

                } // end access granted
                else if (!user.AccessGrantedToFacility)
                {
                    ViewBag.AccessDeniedMsgRecieved = false;
                    user.TimeAccessDenied = DateTime.Now;
                }

                // if door has been opened and user is authorised
                if (facility.DoorOpened && user.AccessGrantedToFacility)
                    // log the time granted, and wait 5 seconds.
                    System.Threading.Thread.Sleep(5000);

                // When 5 second timer finishes, we close the door again automatically
                facility.IsOpenDoorRequested = false;
                ViewBag.IsOpenDoorRequested = false;
                facility.DoorOpened = false;

                _facilityContext.Update(facility);
                // if we are entering gym, use the new facility object, if we are leaving, use the facility detail using Db values.
                if (enteredGym) _facilityContext.Update(currentFacilityDetail);
                // after a facility exist, then we can update facility to avoid foreign key constraint?
                await _facilityContext.SaveChangesAsync();
                await _userManager.UpdateAsync(user);
            }
            return enteredGym;
        }

        private async Task LeaveGym(Facility facilityView, ApplicationUser user, Facility facility, List<UsersInGymDetail> facilityDetails, UsersInGymDetail currentFacilityDetailDb, Error Errors)
        {
            string keyName = $"{user.FirstName}_{user.Id}.jpg";

            user.IsInsideGym = false;
            // if it is not 0 then we can decrement to avoid negatives
            if (facility.NumberOfClientsInGym != 0) facility.NumberOfClientsInGym--;

            // adjust all variables to update the user to a left gym status
            if (user.WillUseWeightsRoom)
            {
                facility.NumberOfClientsUsingWeightRoom--;
                user.WillUseWeightsRoom = false;
            }
            if (user.WillUseCardioRoom && facility.NumberOfClientsUsingCardioRoom != 0)
            {
                facility.NumberOfClientsUsingCardioRoom--;
                user.WillUseCardioRoom = false;
            }
            if (user.WillUseStretchRoom && facility.NumberOfClientsUsingStretchRoom != 0)
            {
                facility.NumberOfClientsUsingStretchRoom--;
                user.WillUseWeightsRoom = false;
            }

            // if there are entries for facilities, loop through all the facilities, remove the entry which is stamped with the current user entry
            if (facilityDetails.Count() > 0)
            {
                _facilityContext.UsersInGymDetails.Remove(currentFacilityDetailDb);
            }

            facilityView.IsCameraScanSuccessful = false;
            user.IsWithin10m = false;
            user.IsCameraScanSuccessful = false;
            user.AccessGrantedToFacility = false;

            // delete detected image from S3 bucket
            try
            {
               

                var deleteObjectRequest = new Amazon.S3.Model.DeleteObjectRequest
                {
                    BucketName = bucket,
                    Key = keyName
                };

                await S3Client.DeleteObjectAsync(deleteObjectRequest);
            }
            catch (AmazonS3Exception e)
            {
                _logger.LogInformation(e.Message);
                await SaveErrorToDB(Errors, e, "Delete Source Image", keyName, "no target", bucket, 0f);
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.Message);
                await SaveErrorToDB(Errors, e, "Delete Source Image", keyName, "no target", bucket, 0f );
            }
        }

        // Face detection method
        private async Task FacialRecognitionScan(ApplicationUser user, UsersInGymDetail currentFacilityDetail, Error Errors)
        {
            // initialize similarity threshold for accepting face match, source and target img.
            // S3 bucket img, dynamically selected based on user currently logged in.
            try
            {
                float similarityThreshold = 70F;
                string photo = $"{user.FirstName}_{user.Id}.jpg";
                String targetImage = $"{user.FirstName}_{user.Id}_Target.jpg"; // Remove g from jpg TODO Test logging
                float similarityResult = 2f;
                StringBuilder sbComp = new StringBuilder();
                StringBuilder sbDet = new StringBuilder();

                try
                {
                    // create image objects
                    Image imageSource = new Image()
                    {
                        S3Object = new S3Object()
                        {
                            Name = photo,
                            Bucket = bucket
                        },
                    };
                    sbComp.Append(">Img Src: " + imageSource.S3Object.Name + ", ");

                    Image imageTarget = new Image()
                    {
                        S3Object = new S3Object()
                        {
                            Name = targetImage,
                            Bucket = bucket
                        },
                    };
                    sbComp.Append(">Img Target: " + imageTarget.S3Object.Name + ", ");

                    // create a compare face request object
                    CompareFacesRequest compareFacesRequest = new CompareFacesRequest()
                    {
                        SourceImage = imageSource,
                        TargetImage = imageTarget,
                        SimilarityThreshold = similarityThreshold
                    };
                    sbComp.Append("> Comp face  req: " + compareFacesRequest.ToString() + ", ");

                    // detect face features of img scanned
                    CompareFacesResponse compareFacesResponse = await AmazonRekognition.CompareFacesAsync(compareFacesRequest);
                    sbComp.Append("> Comp face respo: " + compareFacesResponse.ToString() + "Unmatched faces: " + compareFacesResponse.UnmatchedFaces.ToString()
                        + compareFacesResponse.SourceImageFace + compareFacesResponse.HttpStatusCode +  ", ");

                    // Display results
                    foreach (CompareFacesMatch match in compareFacesResponse.FaceMatches)
                    {
                        ComparedFace face = match.Face;
                        // if confidence for similarity is over 90 then grant access
                        if (match.Similarity > 90)
                            // if there is a match set scan success
                            user.IsCameraScanSuccessful = true;
                        else
                            ViewBag.MatchResult = "Facial Match Failed!";
                        similarityResult = match.Similarity + 5f;
                    }

                }
                catch (Exception e)
                {
                    _logger.LogInformation(e.Message);
                    await SaveErrorToDB(Errors, e, "Compare Face", photo, targetImage, bucket, similarityResult,
                        sbComp.ToString());
                }

                // now add get facial details to display in the view.
                DetectFacesRequest detectFacesRequest = new DetectFacesRequest()
                {
                    Image = new Image()
                    {
                        S3Object = new S3Object()
                        {
                            Name = targetImage,
                            Bucket = bucket
                        },
                    },
                    // "DEFAULT": BoundingBox, Confidence, Landmarks, Pose, and Quality.
                    Attributes = new List<String>() { "ALL" }
                };

                try
                {
                    DetectFacesResponse detectFacesResponse = await AmazonRekognition.DetectFacesAsync(detectFacesRequest);
                    sbDet.Append("> Detect face req: " + detectFacesRequest.ToString() + ", ");

                    bool hasAll = detectFacesRequest.Attributes.Contains("ALL");

                    sbDet.Append("> Detect face response: " + detectFacesResponse.ToString() + ", ");

                    foreach (FaceDetail face in detectFacesResponse.FaceDetails)
                    {
                        // if the face found has all attributes within a Detect Face object then save these values to the database.
                        if (hasAll)
                        {
                            currentFacilityDetail.IsSmiling = face.Smile.Value;
                            currentFacilityDetail.Gender = face.Gender.Value.ToString();
                            currentFacilityDetail.AgeRangeLow = face.AgeRange.Low;
                            currentFacilityDetail.AgeRangeHigh = face.AgeRange.High;
                            sbDet.Append(face.Gender.Value.ToString().ToString() + ", ");
                        }
                    }
                   

                }
                catch (Exception e)
                {
                    _logger.LogInformation(e.Message);
                    await SaveErrorToDB(Errors, e, "Facial Details", photo, targetImage, bucket, similarityResult);
                }
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.Message);
                await SaveErrorToDB(Errors, e, "All Facila Scan", string.Empty, string.Empty, bucket, 0f);
            }
        }

        public bool UserInfo { get; set; }
        // return the value of user inside gym
        [HttpGet]
        public async Task<IActionResult> OnSelectCamButton()
        {

        var UserInfo = await _userManager.GetUserAsync(User);


            if (UserInfo.Id == null) return NotFound();

            var UserInfoVM = UserInfo.IsInsideGym;

            return View(UserInfoVM);
        }

            public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> ErrorLogs()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.Id == null) return RedirectToPage("/Identity/Account/Login");
            // TODO: Change this to not be hard codede
            string mainAdminID = "9d816230-f47f-42df-bfbc-5e5431274301";

            if (user.Id == mainAdminID) return View(await _facilityContext.Errors.ToListAsync());

            return RedirectToAction(nameof(Index));
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteLogs()  //TODO
        //{
        //    //var user = await _userManager.GetUserAsync(User);

        //    //if (user.Id == null) return RedirectToPage("/Identity/Account/Login");
        //    //// TODO: Change this to not be hard codede
        //    //string mainAdminID = "9d816230-f47f-42df-bfbc-5e5431274301";

        //    //if (user.Id == mainAdminID)
        //    //{
        //    //    await _facilityContext.Errors.RemoveRange()
        //    //    return View(await _facilityContext.Errors.ToListAsync());
        //    //}

        //    //return RedirectToAction(nameof(Index));
        //}

        private async Task SaveErrorToDB(Error Errors, Exception e, string argCheckType, string argSrcImg, string argTrgImg, string argBucket, float argMatchSim)
        {
            var errorMessage = new Error();
            var entry = _facilityContext.Add(errorMessage);
            Errors.ErrorMessage = $"ERROR! An error occured: {e.Message}. | Time of ERROR = {DateTime.Now}. | Source Image:{argSrcImg}. " +
                $"| Target Image: {argTrgImg}. Bucket: {argBucket}. | Match Similarity: {argMatchSim} |. Type of Error: {argCheckType}.";
            entry.CurrentValues.SetValues(Errors);
            await _facilityContext.SaveChangesAsync();
        }

        private async Task SaveErrorToDB(Error Errors, Exception e, string argCheckType, string argSrcImg, string argTrgImg, string argBucket, float argMatchSim, string argDetailedError)
        {
            var errorMessage = new Error();
            var entry = _facilityContext.Add(errorMessage);
            Errors.ErrorMessage = $"ERROR! An error occured: {e.Message}. | Time of ERROR = {DateTime.Now}. | Source Image:{argSrcImg}. " +
                $"| Target Image: {argTrgImg}. Bucket: {argBucket}. | Match Similarity: {argMatchSim} |. Type of Error: {argCheckType}." +
                $"| Detailed Error Logs: {argDetailedError}";
            entry.CurrentValues.SetValues(Errors);
            await _facilityContext.SaveChangesAsync();
        }


        private bool FacilityExists(int id)
        {
            return _facilityContext.Facilities.Any(e => e.FacilityID == id);
        }
    }
}
