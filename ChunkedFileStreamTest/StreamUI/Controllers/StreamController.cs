using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Microsoft.Net.Http.Headers;

namespace StreamUI.Controllers
{
    public class StreamController : Controller
    {
        private static HttpClient Client { get; } = new HttpClient();

        public IActionResult Index()
        {
            return View();
        }

        
        [HttpGet]
        public async Task<FileStreamResult> GetCleary()
        {
            var stream = await Client.GetStreamAsync("https://raw.githubusercontent.com/StephenClearyExamples/AsyncDynamicZip/master/README.md");

            return new FileStreamResult(stream, new MediaTypeHeaderValue("text/plain"))
            {
                FileDownloadName = "README.md"
            };
        }

        public string PlayVideo()
        {
            return "Play video";
        }
    }
}