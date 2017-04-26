using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System.IO;

namespace UWPCameraCapandOCR
{
    class ComputerVisionHelper
    {
        static VisionServiceClient visionServiceClient;

        public static void Initialize(string connectionString)
        {

            try
            {
                visionServiceClient = new VisionServiceClient(connectionString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(Utils.FormatExceptionMessage(ex));
                
                throw ex;
            }

        }

        public async static Task<OcrResults> SendImageToOCR(Stream stream)
        {
            OcrResults ocrResult;

            try
            {
                ocrResult = await visionServiceClient.RecognizeTextAsync(stream);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(Utils.FormatExceptionMessage(ex));

                throw ex;
            }

            return ocrResult;

        }

    }
}
