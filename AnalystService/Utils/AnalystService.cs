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
using System.Text.Json;

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

        public async Task<List<int>> ParseMinPriceAsync(string skuId)
        {
            string url = $"https://halykmarket.kz/api/public/v1/product/allMerchantOffersV2?skuId={skuId}&page=1&size=20&paymentType=LOAN&legacySort=false&sortBy=DEFAULT&merchantName=&deliveryPeriod=&officialDistributorOnly=false";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/json, text/plain, */*");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            request.Headers.Add("Referer", $"https://halykmarket.kz/product/{skuId}");
            request.Headers.Add("Accept-Language", "ru");
            request.Headers.Add("CityCode", "750000000");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("merchantInfoList", out var merchants))
                    return null;

                var prices = merchants.EnumerateArray()
                    .Select(m => m.GetProperty("price").GetDecimal())
                    .Select(p => (int)p)
                    .ToList();

                return prices;
            }
            catch
            {
                return null;
            }
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


        public async Task SendNotificationAsync(Guid userId, string productName, double oldPrice, double newPrice, string productUrl)
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


                var message = $"Цена на *{productName}*: {oldPrice} ₸ → {newPrice} ₸\n[Ссылка]( https://halykmarket.kz{productUrl})";

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

        public async Task SendNotificationAdminAsync(string message)
        {
            try
            {
                string _telegramBotToken = "7980379241:AAFnd_6NuI3PAlUPomo-4T42vyfZe_i7vuI";

                long chatId = 8053567705;

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