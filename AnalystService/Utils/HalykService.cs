using ClientService.EF;
using ClientService.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClientService.Utils
{

    public class HalykService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<HalykService> _logger;
        public HalykService(HttpClient httpClient, ILogger<HalykService> logger, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _logger = logger;
            _context = context;
        }

        public async Task<string?> LoginAsync(string email, string password)
        {
            var url = "https://halykmarket.kz/gw/merchant-orders/auth";
            var payload = new LoginRequest
            {
                Email = email,
                Password = password
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, payload);

                _logger.LogInformation("Status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    _logger.LogInformation("Token: {Token}", data?.Token);
                    return data?.Token;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Ошибка авторизации: {Error}", error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при выполнении запроса: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<List<Setting>> GetSellerProductsAsync(string email, string password, string phoneNumber, Guid userId)
        {
            List<Setting> allProduct = new List<Setting>();

            var token = await LoginAsync(email, password); //TODO Нужно сохранить в статике
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Не удалось получить токен.");
                return new();
            }

            var headers = _httpClient.DefaultRequestHeaders;
            headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var allProducts = new List<Dictionary<string, object>>();
            int page = 1;

            // Шаг 1: Получить список всех продуктов
            while (true)
            {
                var url = $"https://halykmarket.kz/gw/merchant/merchant/product?q=&page={page}&status=ON_SALE";
                var response = await _httpClient.GetAsync(url);

                _logger.LogInformation("Page {Page}, Status: {Status}", page, response.StatusCode);

                if (!response.IsSuccessStatusCode) break;

                var data = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                if (data == null || !data.TryGetValue("content", out var contentObj)) break;

                var contentList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(contentObj.ToString());
                if (contentList == null || contentList.Count == 0) break;

                allProducts.AddRange(contentList);
                page++;
            }

            // Шаг 2: Получить цену для каждого продукта
            int counter = 1;
            foreach (var product in allProducts)
            {
                if (!product.TryGetValue("id", out var idObj)) continue;
                var productId = idObj.ToString();

                var fullInfoUrl = $"https://halykmarket.kz/gw/merchant/merchant/product/full-info?merchant_product_id={productId}&status=APPROVED&sub_status=ON_SALE";
                var fullResp = await _httpClient.GetAsync(fullInfoUrl);

                if (!fullResp.IsSuccessStatusCode) continue;

                var fullInfo = await fullResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();

                try
                {
                    var remainingInfoRaw = fullInfo["remainingInfo"].ToString();
                    var remainingInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(remainingInfoRaw);

                    var merchantProductCode = remainingInfo["merchantProductCode"].ToString();
                    var loanPeriod = ((JsonElement)remainingInfo["loanPeriod"]).GetInt32();

                    var pointByCityRaw = remainingInfo["pointByCity"].ToString();
                    var pointByCity = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(pointByCityRaw);

                    foreach (var cityEntry in pointByCity)
                    {
                        var city = JsonSerializer.Deserialize<Dictionary<string, object>>(cityEntry["city"].ToString());
                        var points = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(cityEntry["points"].ToString());
                        var price = ((JsonElement)cityEntry["price"]).GetDouble();

                        foreach (var point in points)
                        {
                            var fullInfoEntity = new ProductPoint
                            {
                                MerchantProductCode = merchantProductCode,
                                LoanPeriod = loanPeriod,
                                CityCode = ((JsonElement)city["code"]).GetString(),
                                CityName = ((JsonElement)city["name"]).GetString(),
                                CityNameRu = ((JsonElement)city["nameRu"]).GetString(),
                                PointCode = ((JsonElement)point["code"]).GetString(),
                                PointName = ((JsonElement)point["name"]).GetString(),
                                Amount = ((JsonElement)point["amount"]).GetInt32(),
                                Price = price
                            };

                            // проверка на дубликаты (по constraint)
                            var exists = _context.ProductPoints.FirstOrDefault(p => p.MerchantProductCode == fullInfoEntity.MerchantProductCode);

                            if (exists == null) _context.ProductPoints.Add(fullInfoEntity);
                            else
                            {
                                _context.ProductPoints.Remove(exists);
                                _context.ProductPoints.Add(fullInfoEntity);
                            }


                            if (!product.TryGetValue("name", out var nameObj)) continue;
                            var pruductName = nameObj.ToString();

                            if (!product.TryGetValue("imageUrl", out var imageUrlObj)) continue;
                            var pruductImageUrl= imageUrlObj.ToString();

                            if (!product.TryGetValue("marketUrl", out var marketUrlObj)) continue;
                            var pruductMarketUrl = marketUrlObj.ToString();

                            var userSettin = new UserSettings()
                            {
                                UserId = userId,
                                MerchantProductCode = fullInfoEntity.MerchantProductCode,
                                ProductName = pruductName,
                                ActualPrice = fullInfoEntity.Price,
                                ImageUrl = pruductImageUrl,
                                MarketUrl = pruductMarketUrl,
                                Remains = fullInfoEntity.Amount,
                                IsDump = false,
                                MaxPrice = 0,
                                MinPrice = 0
                            };

                            var existsSetting = _context.UserSettings.FirstOrDefault(p => p.MerchantProductCode == fullInfoEntity.MerchantProductCode);

                            if (existsSetting == null) _context.UserSettings.Add(userSettin);
                            else
                            {
                                _context.UserSettings.Remove(existsSetting);
                                _context.UserSettings.Add(userSettin);
                            }


                            await _context.SaveChangesAsync();
                            allProduct.Add(Mapper.ToSetting(userSettin));
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError("Ошибка при сохранении точек продукта {Id}: {Error}", productId, ex.Message);
                }

                if (counter % 6 == 0)
                {
                    await Task.Delay(30000);
                }
                counter++;
            }


            return allProduct;
        }
    }
}
