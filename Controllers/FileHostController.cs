using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest.Serialization;
using QnABot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace QnABot.Controllers
{
    public class FileHostController : ControllerBase
    {
        private readonly FileHost _fileHost;

        public FileHostController(
            FileHost fileHost)
        {
            _fileHost = fileHost;
        }

        [EnableCors("Localhost")]
        [Route("hostfile")]
        [HttpPost]
        public async Task PostAsync(string name, string type)
        {
            var id = Guid.NewGuid().ToString();
            var stream = new MemoryStream();
            await Request.Body.CopyToAsync(stream);
            var data = new FileHosted
            {
                Name = name,
                Type = type,
                Stream = stream,
            };
            _fileHost.Add(id, data);
            using (var writer = new StreamWriter(Response.Body))
            {
                await writer.WriteAsync(id);
            }
        }

        [EnableCors("QnA")]
        [Route("hostfile")]
        [HttpGet]
        public async Task<IActionResult> GetAsync(string id)
        {
            var data = _fileHost.GetNotExpired(id);
            return File(data.GetStreamCopy(), data.Type, data.Name);
        }
    }
}
