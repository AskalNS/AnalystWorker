using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Web;
using System.Linq;
using System.Web;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using System;
using ClientService.EF;
using ClientService.Models;

namespace Analyst.Utils
{
    class AnalystService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AnalystService> _logger;

        public AnalystService(HttpClient httpClient, ApplicationDbContext dbContext, ILogger<AnalystService> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<int?> ParseMinPriceAsync(string productQuery)
        {
            string query = AnalystUtils.SimplifyQuery(productQuery);
            string url = $"https://halykmarket.kz/search?r46_search_query={HttpUtility.UrlEncode(query)}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var html = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var priceNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'h-product-card__price')]");
            if (priceNodes == null) return null;

            var prices = priceNodes
                .Select(node => node.InnerText.Replace("₸", "").Replace(" ", "").Trim())
                .Select(text => int.TryParse(text, out var price) ? price : (int?)null)
                .Where(p => p.HasValue)
                .Select(p => p.Value)
                .ToList();

            return prices.Any() ? prices.Min() : (int?)null;
        }



        public async Task<string?> LoginAsync(string login, string password)
        {
            try
            {
                var url = "https://halykmarket.kz/gw/merchant-orders/auth";

                var payload = new
                {
                    login,
                    password
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<LoginResponse>(json);
                    return data?.Token;
                }

                _logger.LogWarning($"Ошибка входа: статус {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при логине: {ex.Message}");
            }

            return null;
        }


        public async Task SendNotificationAsync(Guid userId, string productName, double oldPrice, int newPrice, string productUrl)
        {
            try
            {
                string _telegramBotToken = "7980379241:AAFnd_6NuI3PAlUPomo-4T42vyfZe_i7vuI";

                var chatStringId = _dbContext.AdditionalUserInfo
                    .Where(u => u.UserId == userId)
                    .Select(u => u.TelegramId)
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(chatStringId)) return;

                long chatId = Convert.ToInt64(chatStringId);


                var message = $"Цена на *{productName}*: {oldPrice} ₸ → {newPrice} ₸\n[Ссылка]({productUrl})";

                var payload = new
                {
                    chat_id = chatId,
                    text = message,
                    parse_mode = "Markdown",
                    disable_web_page_preview = true
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"https://api.telegram.org/bot{_telegramBotToken}/sendMessage", content);

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка отправки Telegram уведомления: {ex.Message}");
            }
        }
    }
}