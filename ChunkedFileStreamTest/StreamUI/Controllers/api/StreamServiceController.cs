using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ChunkedFileStreamTest;

namespace StreamUI.Controllers.api
{
    [Route("api/[controller]")]
    public class StreamServiceController : Controller
    {
        [HttpGet]
        public StreamResult GetVideo()
        {
            try
            {
                StreamFile sf = new StreamFile();
                bool containHeaderInfo = HttpContext.Request.Headers.ContainsKey("Accept");

                if (containHeaderInfo)
                {
                    Microsoft.Extensions.Primitives.StringValues headers;
                    if (HttpContext.Request.Headers.TryGetValue("Accept", out headers))
                    {
                        // Check for vide and wild card. Only firefox had the actual video type
                        if (headers.ToString().Contains("video") || headers.ToString().Contains("*/*"))
                        {
                            StreamResult sr = sf.GetVideoStream();
                            return ((sr == null) ? sf.GetEmptyStream() : sr);
                        }
                    }
                }

                return sf.GetEmptyStream();
            }
            catch(Exception ex)
            {
                throw ex;
            }

        }
    }
}