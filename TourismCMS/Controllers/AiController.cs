using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace TourismCMS.Controllers
{
    public class AiController : Controller
    {
        private readonly IMemoryCache _cache;

        public AiController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [HttpPost("/ai/enhance-description")]
        public async Task<IActionResult> EnhanceDescription([FromForm] string text, [FromForm] string role)
        {
            // Simple placeholder: append a short positive adjective if allowed
            // Rate limiting: owner = 10/day, admin = unlimited
            // For demo, we won't enforce per-user limits here; implement using cache or DB in production

            if (string.IsNullOrWhiteSpace(text))
            {
                return BadRequest(new { error = "Text is required" });
            }

            // Simulate AI processing with a short delay
            await Task.Delay(500);

            var enhanced = text;
            if (!text.Contains("tuyệt"))
            {
                enhanced = text + " Đây là một mô tả hấp dẫn và tích cực, phù hợp để thu hút khách tham quan.";
            }

            if (enhanced.Length > 300) enhanced = enhanced.Substring(0, 300);

            return Ok(new { result = enhanced });
        }
    }
}