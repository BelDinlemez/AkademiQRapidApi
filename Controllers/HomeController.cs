using AkademiQRapidApi.Models;
using AkademiQRapidApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AkademiQRapidApi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiService _apiService;

        public HomeController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var dashboard = new DashboardViewModel();

            var tasks = new List<Task>
            {
                   Task.Run(async () => dashboard.CurrencyRates = await _apiService.GetCurrencyRatesAsync()),
   Task.Run(async () => dashboard.FuelPrices = await _apiService.GetFuelPricesAsync()),

    Task.Run(async () => dashboard.MovieOfTheDay      = await _apiService.GetMovieOfTheDayAsync()),
    //Task.Run(async () => dashboard.IstanbulWeather = await _apiService.GetIstanbulWeatherAsync()),
    Task.Run(async () => dashboard.AnkaraWeather = await _apiService.GetAnkaraWeatherAsync()),
    Task.Run(async () => dashboard.SportsScores = await _apiService.GetSportsScoresAsync()),
    Task.Run(async () => dashboard.TurkishNews = await _apiService.GetTurkishNewsAsync()),
Task.Run(async () => { var r = await _apiService.GetCryptoBtcAsync();
                       dashboard.CryptoQuote = r.quote;
                       dashboard.CryptoSeries = r.series; }),



            };
          

            await Task.WhenAll(tasks);

            return View(dashboard);
        }
    }
}
