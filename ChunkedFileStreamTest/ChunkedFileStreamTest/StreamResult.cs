
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ChunkedFileStreamTest
{
    public class StreamResult : FileStreamResult
    {
        // default buffer size as defined in BufferedStream type
        //private const int BufferSize = 0x1000 - changed to 2048;
        private const int BufferSize = 131072;
        private string MultipartBoundary = "<qwe123>";
        private long Heartbeat = 1;

        public StreamResult(Stream fileStream, string contentType)
            : base(fileStream, contentType)
        {

        }

        public StreamResult(Stream fileStream, MediaTypeHeaderValue contentType)
            : base(fileStream, contentType)
        {

        }

        public StreamResult(Stream fileStream, MediaTypeHeaderValue contentType, long heartbeat)
            : base(fileStream, contentType)
        {
            Heartbeat = heartbeat;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            await WriteFileAsync(context.HttpContext.Response);
        }

        
        /// <summary>
        /// Constructs the VideoResult asynchronous stream that will go down to the client
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        protected async Task WriteFileAsync(HttpResponse response)
        {
            try
            {
                var bufferingFeature = response.HttpContext.Features.Get<IHttpBufferingFeature>();
                bufferingFeature?.DisableResponseBuffering();
                RangeHeaderValue rangeHeader = response.HttpContext.Request.GetTypedHeaders().Range;
                var length = FileStream.Length;
                long start = 0;
                long end = 0;

                response.Headers.Add("Accept-Ranges", "bytes");

                // The filestream length is 0, there is not content, handle accordingly
                if (length == 0)
                {
                    response.StatusCode = (int)HttpStatusCode.NoContent;
                    response.ContentType = ContentType.ToString();

                    await FileStream.CopyToAsync(response.Body);
                }

                // when no range was found return empty or there are more than 1 - not tested yet
                if (rangeHeader == null || rangeHeader.Unit != "bytes" || rangeHeader.Ranges.Count > 1
                || rangeHeader.Ranges.Count <= 0
                || !TryReadRangeItem(rangeHeader.Ranges.First(), length, out start, out end))
                {
                    // Sent byte range header down to client, so that browser know in the next request to include it
                    // Statuscode must be OK in order for request to be successfully handled. This is a fix for IE that does not send range header
                    // Length and content type must also be in reponse
                    response.ContentLength = length;
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.ContentType = ContentType.ToString();
                    await FileStream.CopyToAsync(response.Body);
                }

                if (response.HttpContext.Request.GetTypedHeaders().Range == null)
                {
                    await response.Body.FlushAsync();
                }
                else
                {
                    // only use the first found range - not using multipart requests
                    RangeItemHeaderValue range = response.HttpContext.Request.GetTypedHeaders().Range.Ranges.First();

                    // Set all applicable reponse headers, they are all required for streaming to work
                    response.ContentType = ContentType.ToString();

                    response.StatusCode = (int)HttpStatusCode.PartialContent;
                    var contentRange = new ContentRangeHeaderValue(start, end, length);

                    // This is NB in order to seek properly
                    response.Headers.Add("Content-Range", $"bytes {contentRange.From}-{contentRange.To}/{contentRange.Length}");
                    await WriteDataToResponseBody(start, end, response);
                }
            }
            catch (Exception ex)
            {
                response.HttpContext.Request.Headers.Add("Accept-Ranges", "bytes");
                response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
            }
        }

        /// <summary>
        /// Constructs the actual stream and places the byte packages into the reponse body.
        /// Reades to file and places those chunks into the response
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private async Task WriteDataToResponseBody(long start, long end, HttpResponse response)
        {
            int contentLength = (int)(end - start + 1);
            int count = 0;
            int remainingBytes = 0;
            byte[] buffer = new byte[BufferSize];
            response.ContentLength = contentLength;
            long checkRedis = DateTime.Now.Ticks;
            int bufferOverrun = 0;

            
            FileStream.Seek(start, SeekOrigin.Begin);
            
            do
            {
                try
                {
                    // redis is used here to store a unique value per stream in order to stop downloading the stream when paused or ahead by 10s
                    TimeSpan elapsedSpan = new TimeSpan(DateTime.Now.Ticks - checkRedis);

                    // Only check the redis cache every second
                    if (elapsedSpan.TotalSeconds > 1)
                    {
                            bufferOverrun = 0;
                    }

                    if (Heartbeat == 0)
                    {
                        // When there is no data to send, just sleep 20ms. No need to be very active in sending data
                        Thread.Sleep(20);
                        checkRedis = DateTime.Now.Ticks;
                        response.Body.Write(buffer, 0, 0);
                    }
                    else
                    {
                        remainingBytes = (int)(end - FileStream.Position + 1);
                        System.Diagnostics.Debug.WriteLine ("position : "+ FileStream.Position + " | end : " + end + " | start : " + start  + " | remainingBytes: "+ remainingBytes);
                        if (remainingBytes > BufferSize)
                            count = FileStream.Read(buffer, 0, BufferSize);
                        else
                            count = FileStream.Read(buffer, 0, remainingBytes);

                        await response.Body.WriteAsync(buffer, 0, count);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            } while (FileStream.Position <= end);
        }

        /// <summary>
        /// Parses the Range header from the request in order to know whats byte ranges to send down
        /// to the client, important for sending chunked data
        /// </summary>
        /// <param name="range"></param>
        /// <param name="contentLength"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private static bool TryReadRangeItem(RangeItemHeaderValue range, long contentLength, out long start, out long end)
        {
            if (range.From != null)
            {
                start = range.From.Value;
                if (range.To != null)
                    end = range.To.Value;
                else
                    end = contentLength - 1;
            }
            else
            {
                end = contentLength - 1;
                if (range.To != null)
                    start = contentLength - range.To.Value;
                else
                    start = 0;
            }
            return (start < contentLength && end < contentLength);
        }
    }
}
