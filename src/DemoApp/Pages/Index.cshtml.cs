using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace DemoApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly Random _random = new();

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public async Task OnGet()
        {
            var tasks = Enumerable.Range(1, 5).Select(t => Task.Run(WriteRandom)).ToArray();
            await Task.WhenAll(tasks);
        }

        public void WriteRandom()
        {
            for (var i = 0; i < 1000; i++)
            {
                _logger.LogInformation(CreateRandomString(100));
            }
        }

        public string CreateRandomString(int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];

            for (var i = 0; i < stringChars.Length; i++)
                stringChars[i] = chars[_random.Next(chars.Length)];

            return new string(stringChars);
        }
    }
}