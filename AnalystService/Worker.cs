using Analyst.Utils;
using ClientService.EF;
using ClientService.Models;
using ClientService.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Text;
using System;
using System.Diagnostics;

namespace Analyst
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HttpClient _httpClient;

        public Worker(ILogger<Worker> logger, HttpClient httpClient, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _httpClient = httpClient;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var cycleStart = DateTime.UtcNow;
                try
                {
                    using var scope = _scopeFactory.CreateScope(); // создаём временный скоуп

                    var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var _analystService = scope.ServiceProvider.GetRequiredService<AnalystService>();
                    var _encryptionUtils = scope.ServiceProvider.GetRequiredService<EncryptionUtils>();
                    var _halykService = scope.ServiceProvider.GetRequiredService<HalykService>();



                    List<IntegratedUsers> users = _context.IntegratedUsers.ToList();

                    // По каждому пользователю
                    foreach (var user in users)
                    {


                        if (DateTime.Now - user.TimeFlag < TimeSpan.FromMinutes(10)) continue;
                        if (user.IsWorking) continue;

                        user.TimeFlag = DateTime.UtcNow;
                        user.IsWorking = true;
                        _context.SaveChanges();


                        try
                        {

                            #region Получение HalykCredentials

                            HalykCredential credentils = _context.HalykCredentials.Where(x => x.UserId == user.UserId).First();
                            if (credentils == null)
                            {
                                User us = _context.Users.Where(x => x.Id == user.UserId).First();
                                if (us == null) continue;

                                logEndSend("У пользователя:" + us.PhoneNumber + " нет данных маркета", _analystService  );
                                continue; 
                            }

                            string login = credentils.Login;
                            string password = _encryptionUtils.Decrypt(credentils.EncriptPassword, user.UserId);

                            #endregion



                            List<UserSettings> userSettings = _context.UserSettings.Where(x => x.UserId == user.UserId).ToList();
                            foreach (var userSetting in userSettings)
                            {
                                try
                                {
                                    if (!userSetting.IsDump) continue;
                                    if (userSetting.ActualPrice == 0) continue;


                                    ProductArticuls articul = _context.ProductArticuls.Where(x => x.productName == userSetting.ProductName).First();
                                    if (articul.Articule == null)
                                    {
                                        User us = _context.Users.Where(x => x.Id == user.UserId).First();
                                        if (us == null) continue;

                                        logEndSend("У пользователя:" + us.PhoneNumber + " нет артикула для товара: " + userSetting.ProductName, _analystService);
                                        continue; 
                                    }


                                    #region Получение и вычисление мин праиса, место

                                    List<int> prices = await _analystService.ParseMinPriceAsync(articul.Articule);
                                    if (prices.Count <= 1) continue;
                                    int minPrice = prices.Min();

                                    int place = 1;
                                    for (int i = 1; i < prices.Count; i++)
                                    {
                                        if (prices[i] < userSetting.ActualPrice) place++;
                                    }

                                    userSetting.Place = place;
                                    _context.SaveChanges();

                                    double mewPrice;


                                    if (minPrice >= userSetting.ActualPrice) continue;
                                    if (minPrice < (userSetting.fitstMarketPrice * 0.5)) continue;

                                    if (userSetting.MinPrice != 0)
                                    {
                                        if (userSetting.MinPrice > minPrice)
                                        {
                                            if (userSetting.ActualPrice == userSetting.MinPrice) continue;
                                            mewPrice = userSetting.MinPrice;
                                        }
                                        else
                                        {
                                            mewPrice = minPrice - 5;
                                        }
                                    }
                                    else
                                    {
                                        double minProtPrice = (100 - user.MaxPersent) * userSetting.lastMarketPrice / 100;
                                        if (minProtPrice > minPrice)
                                        {
                                            if (userSetting.ActualPrice == minProtPrice) continue;
                                            mewPrice = minProtPrice;
                                        }
                                        else
                                        {
                                            mewPrice = minPrice - 5;
                                        }
                                    }

                                    #endregion




                                    #region Сохранени мин праиса

                                    var productPoints = await _context.ProductPoints
                                                                        .Where(p => p.MerchantProductCode == userSetting.MerchantProductCode)
                                                                        .ToListAsync();

                                    if (!productPoints.Any()) continue;

                                    foreach (var point in productPoints)
                                        point.Price = mewPrice;

                                    await _context.SaveChangesAsync(); ///

                                    var pointByCity = productPoints
                                        .GroupBy(p => p.CityCode)
                                        .Select(g => new
                                        {
                                            city = new
                                            {
                                                name = g.First().CityName,
                                                nameRu = g.First().CityNameRu,
                                                code = g.Key
                                            },
                                            price = mewPrice,
                                            points = g.Select(p => new
                                            {
                                                code = p.PointCode,
                                                name = p.PointName,
                                                amount = p.Amount
                                            }).ToList()
                                        }).ToList();

                                    var payload = new
                                    {
                                        loanPeriod = 24,
                                        merchantProductCode = userSetting.MerchantProductCode,
                                        pointByCity = pointByCity
                                    };



                                    string? token = await _halykService.LoginAsync(login, password);

                                    if (string.IsNullOrEmpty(token)) continue;

                                    var request = new HttpRequestMessage(HttpMethod.Put, "https://halykmarket.kz/gw/merchant/product/remaining")
                                    {
                                        Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
                                    };
                                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                                    var response = await _httpClient.SendAsync(request);

                                    if (!response.IsSuccessStatusCode)
                                    {
                                        var body = await response.Content.ReadAsStringAsync();
                                        logEndSend($"Ошибка PUT-запроса: статус {response.StatusCode}, ответ: {body}", _analystService);
                                    }
                                    else
                                    {
                                        _analystService.SendNotificationAsync(user.UserId, userSetting.ProductName, userSetting.ActualPrice, mewPrice, userSetting.MarketUrl);
                                        userSetting.ActualPrice = mewPrice;

                                        string message = $"Успешно обновлена цена для {userSetting.MerchantProductCode} ? {mewPrice} ?";
                                        _logger.LogInformation(message);
                                    }
                                    await _context.SaveChangesAsync();

                                    #endregion

                                }
                                catch (Exception ex)
                                {
                                    logEndSend("Setting layer - " + ex.Message, _analystService);
                                }
                            }

                        }
                        catch(Exception e)
                        {
                            logEndSend("User layer - " + e.Message, _analystService);
                        }
                        finally
                        {
                            user.IsWorking = false;
                            _context.SaveChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("ERROR:" + ex.Message);
                }
                // Пауза если цикл закончился слишком быстро
                var elapsed = DateTime.UtcNow - cycleStart;
                if (elapsed < TimeSpan.FromMinutes(2))
                {
                    var delay = TimeSpan.FromMinutes(2) - elapsed;
                    _logger.LogInformation($"Цикл занял {elapsed.TotalSeconds:F1} сек, спим {delay.TotalSeconds:F1} сек.");
                    await Task.Delay(delay, stoppingToken);
                }
                else
                {
                    _logger.LogInformation($"Цикл занял {elapsed.TotalSeconds:F1} сек, спим 1 минуту.");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }


        private void logEndSend(string message, AnalystService _analystService)
        {
            _logger.LogInformation(message);
            _analystService.SendNotificationAdminAsync(message);

        }
    }
}
