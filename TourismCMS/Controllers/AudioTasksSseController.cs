using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace TourismCMS.Controllers
{
    public class AudioTasksSseController : Controller
    {
        // GET: /sse/audio-tasks
        [HttpGet("/sse/audio-tasks")]
        public async Task Get()
        {
            Response.Headers.Add("Content-Type", "text/event-stream");

            for (int i = 0; i <= 100; i += 10)
            {
                var data = $"data: {i}\n\n";
                var bytes = Encoding.UTF8.GetBytes(data);
                await Response.Body.WriteAsync(bytes, 0, bytes.Length);
                await Response.Body.FlushAsync();
                await Task.Delay(500);
            }
        }
    }
}