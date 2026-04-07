using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace TourismCMS.Controllers
{
    public class ChatRequest { public string? prompt { get; set; } }

    [AllowAnonymous]
    public class AiController : Controller
    {
        private readonly IMemoryCache _cache;

        public AiController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [AllowAnonymous]
        [HttpPost("/api/ai/chat")]
        [HttpPost("/ai/chat")]
        public IActionResult Chat([FromBody] ChatRequest req)
        {
            return Ok("Dạ, AI hiện đang được bảo trì nhưng cảm ơn bạn đã hỏi: " + req?.prompt);
        }

        [AllowAnonymous]
        [HttpPost("/ai/enhance-description")]
        public async Task<IActionResult> EnhanceDescription([FromForm] string text, [FromForm] string role)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return BadRequest(new { error = "Text is required" });
            }

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
