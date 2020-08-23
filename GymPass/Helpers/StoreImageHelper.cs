using GymPass.Data;
using GymPass.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace GymPass.Helpers
{
    public class StoreImageHelper
    {
        private readonly FacilityContext _facilityContext;
        private string keyName;

        public StoreImageHelper(FacilityContext facilityContext)
        {
            _facilityContext = facilityContext;
        }

        /// <summary>  
        /// Saving captured image into Folder.  
        /// </summary>  
        /// <param name="file"></param>  
        /// <param name="fileName"></param> 
        public void StoreInFolder(IFormFile file, string fileName)
        {
            using (FileStream fs = File.Create(fileName))
            {
                file.CopyTo(fs);
                fs.Flush();
            }
        }

        /// <summary>  
        /// Saving captured image into Folder.  
        /// </summary>  
        /// <param name="file"></param>  
        /// <param name="fileName"></param> 
        public void DeleteFromFolder(string filePath)
        {
            File.Delete(filePath);
        }

        /// <summary>  TODO: Try to put store image in database here instead of controller.
        /// Saving captured image into database.  
        /// </summary>  
        /// <param name="imageBytes"></param>  
        //public  void StoreInDatabase(byte[] imageBytes)
        //{


        //    try
        //    {
        //        if (imageBytes != null)
        //        {
        //            string base64String = Convert.ToBase64String(imageBytes, 0, imageBytes.Length);
        //            string imageUrl = string.Concat("data:image/jpg;base64,", base64String);
        //            ImageStore imageStore = new ImageStore()
        //            {
        //                CreateDate = DateTime.Now,
        //                ImageBase64String = imageUrl,
        //                ImageId = 0,
        //                UniqueID = Convert.ToString(Guid.NewGuid())
        //        };
        //            _facilityContext.ImageStore.Add(imageStore);
        //            _facilityContext.SaveChanges();
        //        }
        //    }
        //    catch (Exception)
        //    {

        //    }
    }
}

