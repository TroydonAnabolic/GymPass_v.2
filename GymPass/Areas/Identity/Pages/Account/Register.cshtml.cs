using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using GymPass.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using GymPass.Data;
using GymPass.Helpers;
using System.IO;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Hosting;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace GymPass.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly FacilityContext _facilityContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const string bucketName = "gym-user-bucket-i";
        private readonly IAmazonS3 S3Client;

        public RegisterModel(
            IWebHostEnvironment webHostEnvironment,
            IAmazonS3 s3Client,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            FacilityContext facilityContext,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _webHostEnvironment = webHostEnvironment;
            S3Client = s3Client;
            _facilityContext = facilityContext;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        [BindProperty]
        public UsersOutOfGymDetails UsersOutOfGymDetails { get; set; }
        public string ReturnUrl { get; set; }
        [BindProperty]
        public string RegisterURL { get; set; }
        [BindProperty]
        public Error Errors { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")] 
            public string ConfirmPassword { get; set; }

            // TODO: Option to select default gym on sign up
            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Select Default Gym")]
            public int SelectDefaultGym { get; set; }

            [Display(Name = "Test Latitude")]
            public string TestLat { get; set; }
            [Display(Name = "Test Longitude")]
            public string TestLong { get; set; }
            [Display(Name = "User Image")]
            public byte[] UserImage { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;

            RegisterURL = Request.GetDisplayUrl();

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { 
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    DefaultGym = _facilityContext.Facilities.FirstOrDefault().FacilityID, // hard code to be the first gym created
                    TestLat = Input.TestLat,
                    TestLong = Input.TestLong,
                    UserImage = Input.UserImage
                };
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        // Enter facility details and users out of gym details
                        await AddFacilityDetails(user);

                        // add the image to the database
                        await AddImageToDbInByteArr(user);

                        // Ensure no more than 100 S3 object to avoid bill shock
                        if (!await EnsureMax100ObjectsAsync())
                            // Create a Target img and save to the S3 bucket, to be used as verification ( email to be sent to admin to approve, or have an admin page to either complete registration or by setting prop isVerifiedUser to true
                            await AddTargetImageToS3Bucket(user);
                        // now sign in and redirect
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            // If we got this far, something failed, redisplay form
            return Page();
        }

        private async Task AddImageToDbInByteArr(ApplicationUser user)
        {
            if (Request.Form.Files.Count > 0)
            {
                IFormFile file = Request.Form.Files.FirstOrDefault();
                using (var dataStream = new MemoryStream())
                {
                    await file.CopyToAsync(dataStream);
                    user.UserImage = dataStream.ToArray();
                }
                await _userManager.UpdateAsync(user);
            }
        }

        private async Task<bool> EnsureMax100ObjectsAsync()
        {
            bool reachedMaxedObjects = false;
            try
            {

                ListObjectsV2Request request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    MaxKeys = 10
                };
                ListObjectsV2Response response;
                do
                {
                    response = await S3Client.ListObjectsV2Async(request);

                    // Process the response.
                    var numberOfS3Objects = response.S3Objects.Count;
                    reachedMaxedObjects = numberOfS3Objects > 100 ? true : false;

                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                _logger.LogInformation("S3 error occurred. Exception: " + amazonS3Exception);
                await SaveErrorToDB(amazonS3Exception);

            }
            catch (Exception e)
            {
                _logger.LogInformation(e.Message);
                await SaveErrorToDB(e);
            }
            return reachedMaxedObjects;
        }

        private async Task AddFacilityDetails(ApplicationUser user)
        {
            var emptyUserOutOfFacility = new UsersOutOfGymDetails();
            var entry = _facilityContext.Add(emptyUserOutOfFacility);
            UsersOutOfGymDetails.EstimatedTimeToCheck = DateTime.Now;
            UsersOutOfGymDetails.FacilityID = _facilityContext.Facilities.FirstOrDefault().FacilityID;
            UsersOutOfGymDetails.UniqueEntryID = user.Id;
            entry.CurrentValues.SetValues(UsersOutOfGymDetails);
            await _facilityContext.SaveChangesAsync();
        }

        private async Task AddTargetImageToS3Bucket(ApplicationUser user)
        {
            try
            {
                String keyName = $"{user.FirstName}_{user.Id}_Target.jpg";
                var files = HttpContext.Request.Form.Files; // this retrieves file from post request
                                                            //IFormFile file = Request.Form.Files.FirstOrDefault();

                StoreImageHelper storeImageHelper = new StoreImageHelper(_facilityContext) { };
                var fileTransferUtility = new TransferUtility(S3Client);

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        // Getting Filename  
                        var fileName = file.FileName;
                        // Unique filename "Guid"  
                        var myUniqueFileName = Convert.ToString(Guid.NewGuid());
                        // Getting Extension  
                        var fileExtension = Path.GetExtension(fileName);
                        // Concating filename + fileExtension + a key ID based on userID (unique filename)  
                        var newFileName = $"{myUniqueFileName}_{keyName}{fileExtension}";
                        //  Generating Path to store photo   
                        var filepath = Path.Combine(_webHostEnvironment.WebRootPath, "CameraPhotos") + $@"\{newFileName}";

                        if (!string.IsNullOrEmpty(filepath))
                            storeImageHelper.StoreInFolder(file, filepath);

                        // Now save this in the S3 bucket to use for facial recognition
                        try
                        {
                            // grab the filePath from the image captured from the camera
                            using (var fileToUpload =
                                new FileStream(filepath, FileMode.Open, FileAccess.Read))
                            {
                                // upload it (if multiple are taken, they are overriden with the unique userID)
                                await fileTransferUtility.UploadAsync(fileToUpload,
                                                           bucketName, keyName);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogInformation(e.Message);
                            await SaveErrorToDB(e);
                        }

                        // now delete the file to avoid cluttering (in real world, can possibly keep for logs)
                        if (!string.IsNullOrEmpty(filepath))
                            storeImageHelper.DeleteFromFolder(filepath);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.Message);
                await SaveErrorToDB(e);
            }
        }
        private async Task SaveErrorToDB(Exception e)
        {
            var errorMessage = new Error();
            var entry = _facilityContext.Add(errorMessage);
            Errors.ErrorMessage = $"ERROR! An error occured: {e.Message}. Time of ERROR = {DateTime.Now}.";
            entry.CurrentValues.SetValues(Errors);
            await _facilityContext.SaveChangesAsync();
        }
    }
}
