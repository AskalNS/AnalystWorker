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
                try
                {
                    using var scope = _scopeFactory.CreateScope(); // ?? создаём временный скоуп

                    var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var _analystService = scope.ServiceProvider.GetRequiredService<AnalystService>();
                    var _encryptionUtils = scope.ServiceProvider.GetRequiredService<EncryptionUtils>();
                    var _halykService = scope.ServiceProvider.GetRequiredService<HalykService>();



                    List<IntegratedUsers> users = _context.IntegratedUsers.ToList();

                    foreach (var user in users)
                    {
                        user.TimeFlag = DateTime.UtcNow;
                        user.IsWorking = true;
                        _context.SaveChanges();

                        HalykCredential credentils = _context.HalykCredentials.Where(x => x.UserId == user.UserId).First();
                        if (credentils == null)
                        {
                            User us = _context.Users.Where(x => x.Id == user.UserId).First();
                            if (us == null) continue;

                            _logger.LogError("У пользователя:" + us.PhoneNumber + " нет данных маркета");
                            continue; //TODO В будущем добавить функцию уведомления в бд
                        }

                        string login = credentils.Login;
                        string password = _encryptionUtils.Decrypt(credentils.EncriptPassword, user.UserId);

                        List<UserSettings> userSettings = _context.UserSettings.Where(x => x.UserId == user.UserId).ToList();

                        foreach (var userSetting in userSettings)
                        {
                            if (userSetting.ActualPrice == 0) continue;

                            int? minPrice = await _analystService.ParseMinPriceAsync(userSetting.ProductName);
                            if (!minPrice.HasValue) continue;


                            if (minPrice > userSetting.ActualPrice) continue;
                            if (userSetting.MinPrice != 0)
                            {
                                if (userSetting.MinPrice > minPrice)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (((100 - user.MaxPersent) * userSetting.ActualPrice / 100) > minPrice) continue; // TODO Доработать
                            }


                            int mewPrice = minPrice.Value - 5;


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
                                _logger.LogWarning($"Ошибка PUT-запроса: статус {response.StatusCode}, ответ: {body}");
                            }
                            else
                            {
                                _analystService.SendNotificationAsync(user.UserId, userSetting.ProductName, userSetting.ActualPrice, mewPrice, userSetting.ImageUrl);
                                userSetting.ActualPrice = mewPrice;
                                _logger.LogInformation($"Успешно обновлена цена для {userSetting.MerchantProductCode} ? {mewPrice} ?");
                            }
                            await _context.SaveChangesAsync();
                        }


                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("ERROR:" + ex.Message);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
