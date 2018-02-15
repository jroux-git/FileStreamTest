using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChunkedFileStreamTest
{
    public class StreamFile
    {
        //private string filepath = @"C:/temp/0fa8c383-a5b9-4af6-b84f-67b1b2adaf27.mp4";
        private string filepath = @"C:/temp/2015_Super_15_Round_1_Blues_vs_Chiefs_H1.mp4";

        public FileStream GetFileStream()
        {
            return new FileStream(filepath, FileMode.Open, FileAccess.Read);
        }

        public StreamResult GetVideoStream()
        {
            try
            {
                return new StreamResult(GetFileStream(), new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("video/mp4"));
                //return new StreamResult(fileStoreManager.GetChunkedStream(resourceFileGuid, resourceFileLocationPath), new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("video/mp4")); 
            }
            catch (FileNotFoundException ex)
            {
                return GetEmptyStream();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public StreamResult GetEmptyStream()
        {
            Stream emptyStream = new MemoryStream();
            emptyStream.Write(new byte[0], 0, 0);

            return new StreamResult(emptyStream, new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("text/plain"));
        }
    }
}
