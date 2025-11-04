using AkademiQRapidApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net;

namespace AkademiQRapidApi.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;

        // İstersen hostları da appsettings/secrets'tan alabilirsin
        private const string WeatherHost = "weather-api-by-any-city.p.rapidapi.com";
        private const string GasHost = "gas-price.p.rapidapi.com";
        private const string Imdb236Host = "imdb236.p.rapidapi.com";
        private const string FxHost = "exchangerate-api.p.rapidapi.com";
        private const string BasketballHost = "api-basketball.p.rapidapi.com";
        private const string VolleyballHost = "api-volleyball.p.rapidapi.com";

        private string ApiKey => _config["RapidApi:Key"]
    ?? throw new InvalidOperationException("RapidApi:Key is missing");


        public ApiService(IHttpClientFactory httpClientFactory, IMemoryCache cache, IConfiguration config)
        {
            _httpClient = httpClientFactory.CreateClient();
            _cache = cache;
            _config = config;
        }

        #region VOLEYBOL
        public async Task<List<VolleyballMatchViewModel>> GetLastVolleyballMatchesAsync(string? h2h = "7-8", int take = 3)
        {
            var uri = $"https://{VolleyballHost}/games{(string.IsNullOrWhiteSpace(h2h) ? "" : $"/h2h?h2h={h2h}")}";
            var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Add("x-rapidapi-key", ApiKey);
            req.Headers.Add("x-rapidapi-host", VolleyballHost);

            using var res = await _httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync();
            var root = JObject.Parse(json);
            var list = new List<VolleyballMatchViewModel>();

            foreach (var item in root["response"] ?? new JArray())
            {
                var dateStr = (string?)item["date"] ?? "";
                DateTime.TryParse(dateStr, out var parsedDate);

                // Toplam skorlar + period eklemesi (varsa)
                var scores = item["scores"];
                int homeTotal = (int?)scores?["home"] ?? 0;
                int awayTotal = (int?)scores?["away"] ?? 0;

                var periods = item["periods"];
                if (periods is JObject)
                {
                    foreach (var p in periods.Children<JProperty>())
                    {
                        var h = (int?)p.Value?["home"];
                        var a = (int?)p.Value?["away"];
                        if (h.HasValue) homeTotal += h.Value;
                        if (a.HasValue) awayTotal += a.Value;
                    }
                }

                list.Add(new VolleyballMatchViewModel
                {
                    Country = (string?)item["country"]?["name"] ?? "",
                    League = (string?)item["league"]?["name"] ?? "",
                    LeagueLogo = (string?)item["league"]?["logo"] ?? "",
                    HomeTeam = (string?)item["teams"]?["home"]?["name"] ?? "",
                    AwayTeam = (string?)item["teams"]?["away"]?["name"] ?? "",
                    HomeLogo = (string?)item["teams"]?["home"]?["logo"] ?? "",
                    AwayLogo = (string?)item["teams"]?["away"]?["logo"] ?? "",
                    HomeScore = homeTotal,
                    AwayScore = awayTotal,
                    Date = parsedDate == default ? dateStr : parsedDate.ToString("yyyy-MM-dd")
                });
            }

            // son gelenler üstteyse first take; değilse tarihçe sıralayıp al
            return list
                .OrderByDescending(x => x.Date)  // Date string, ama "yyyy-MM-dd" formatlıysa doğru sıralar
                .Take(take)
                .ToList();
        }
        #endregion

        #region HABERLER (TR)
        public async Task<List<NewsItemViewModel>> GetTurkishNewsAsync(int limit = 8)
        {
            // cache: 3 dk
            if (_cache.TryGetValue("news:tr", out List<NewsItemViewModel> cachedNews))
                return cachedNews;

            var host = _config["RapidApi:News:Host"] ?? "currents-news.p.rapidapi.com";
            var uri = $"https://{host}/latest-news?language=tr";

            var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Add("x-rapidapi-key", ApiKey);
            req.Headers.Add("x-rapidapi-host", host);

            using var res = await _httpClient.SendAsync(req);
            if (!res.IsSuccessStatusCode) return new();

            var json = await res.Content.ReadAsStringAsync();
            var root = JObject.Parse(json);
            var news = root["news"] as JArray ?? new JArray();

            var result = news.Take(limit).Select(n => new NewsItemViewModel
            {
                Title = (string?)n["title"] ?? "",
                Description = (string?)n["description"] ?? "",
                Url = (string?)n["url"] ?? "",
                ImageUrl = (string?)n["image"] ?? "",
                Source = (string?)n["author"] ?? (string?)n["source"] ?? "",
                PublishedAt = DateTime.TryParse((string?)n["published"] ?? "", out var dt) ? dt : null
            }).ToList();

            _cache.Set("news:tr", result, TimeSpan.FromMinutes(3));
            return result;
        }
        #endregion

        #region KRİPTO (BTC)
        public async Task<(CryptoQuoteViewModel quote, List<PricePointViewModel> series)> GetCryptoBtcAsync()
        {
            // cache: 1 dk (grafik için yeterli)
            if (_cache.TryGetValue("crypto:btc", out (CryptoQuoteViewModel, List<PricePointViewModel>) cached))
                return cached;

            var host = _config["RapidApi:Crypto:Host"] ?? "coinranking1.p.rapidapi.com";
            var uri = $"https://{host}/coin/Qwsogvtv82FCd?referenceCurrencyUuid=yhjMzLPhuIDl&timePeriod=24h";

            var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Add("x-rapidapi-key", ApiKey);
            req.Headers.Add("x-rapidapi-host", host);

            using var res = await _httpClient.SendAsync(req);
            if (!res.IsSuccessStatusCode) return (new CryptoQuoteViewModel(), new());

            var json = await res.Content.ReadAsStringAsync();
            var root = JObject.Parse(json);
            var coin = root["data"]?["coin"] as JObject ?? new JObject();

            decimal.TryParse((string?)coin["price"] ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var price);
            decimal.TryParse((string?)coin["change"] ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var change);

            var spark = coin["sparkline"] as JArray ?? new JArray();
            var now = DateTime.UtcNow;
            var step = spark.Count > 1 ? TimeSpan.FromHours(24.0 / (spark.Count - 1)) : TimeSpan.FromHours(1);

            var series = new List<PricePointViewModel>();
            for (int i = 0; i < spark.Count; i++)
            {
                if (decimal.TryParse((string?)spark[i], NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
                    series.Add(new PricePointViewModel { Time = now.Add(step * (i - spark.Count + 1)), Price = p });
            }

            var quote = new CryptoQuoteViewModel { Symbol = "BTC", Price = price, Change24h = change };
            var result = (quote, series);

            _cache.Set("crypto:btc", result, TimeSpan.FromMinutes(1));
            return result;
        }
        #endregion

        #region FİLM (IMDb – imdb236)
        public async Task<MovieRecommendationViewModel> GetMovieOfTheDayAsync()
        {
            var req = new HttpRequestMessage(HttpMethod.Get,
                $"https://{Imdb236Host}/api/imdb/search?type=movie&genre=Drama&rows=25&sortOrder=ASC&sortField=id");
            req.Headers.Add("x-rapidapi-key", ApiKey);
            req.Headers.Add("x-rapidapi-host", Imdb236Host);

            using var res = await _httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync();
            var root = JObject.Parse(json);
            var results = root["results"] as JArray;

            if (results == null || results.Count == 0)
                return new MovieRecommendationViewModel { Title = "Veri bulunamadı" };

            var rnd = new Random();
            var pick = results[rnd.Next(results.Count)] as JObject ?? new JObject();

            string imageUrl = "";
            var imageObj = pick["primaryImage"] as JObject;
            if (imageObj?["url"] != null) imageUrl = imageObj["url"]!.ToString();
            else if (pick["thumbnails"] is JArray thumbs && thumbs.Count > 0)
                imageUrl = thumbs[0]?["url"]?.ToString() ?? "";

            return new MovieRecommendationViewModel
            {
                Title = (string?)pick["primaryTitle"] ?? "Bilinmeyen",
                Description = (string?)pick["description"] ?? "",
                ImageUrl = imageUrl,
                Url = (string?)pick["url"] ?? ""
            };
        }
        #endregion

        #region AKARYAKIT
        public async Task<GasPriceViewModel> GetFuelPricesAsync()
        {
            if (_cache.TryGetValue("FuelPrices", out GasPriceViewModel cached))
                return cached;

            var req = new HttpRequestMessage(HttpMethod.Get, $"https://{GasHost}/europeanCountries");
            req.Headers.Add("x-rapidapi-key", ApiKey);
            req.Headers.Add("x-rapidapi-host", GasHost);

            GasPriceViewModel result;
            var res = await _httpClient.SendAsync(req);

            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.TooManyRequests)
                    result = new GasPriceViewModel { Gasoline = 0, Diesel = 0 };
                else
                {
                    res.EnsureSuccessStatusCode(); // throw
                    throw new InvalidOperationException();
                }
            }
            else
            {
                var body = await res.Content.ReadAsStringAsync();
                var root = JObject.Parse(body);

                var tr = ((JArray?)root["results"])
                            ?.FirstOrDefault(x => (string?)x["country"] == "Turkey") as JObject
                         ?? throw new Exception("Türkiye verisi bulunamadı");

                decimal Parse(string? s) =>
                    decimal.TryParse(s?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;

                result = new GasPriceViewModel
                {
                    Gasoline = Parse((string?)tr["gasoline"]),
                    Diesel = Parse((string?)tr["diesel"])
                };
            }

            _cache.Set("FuelPrices", result, TimeSpan.FromMinutes(5));
            return result;
        }
        #endregion

        #region DÖVİZ
        public async Task<ExchangeResultModelView> GetCurrencyRatesAsync()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://{FxHost}/rapid/latest/USD");
            req.Headers.Add("x-rapidapi-key", ApiKey);
            req.Headers.Add("x-rapidapi-host", FxHost);

            using var res = await _httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();

            var body = await res.Content.ReadAsStringAsync();
            var obj = JObject.Parse(body);

            var ratesToken = obj["conversion_rates"] ?? obj["rates"]
                          ?? throw new Exception("Rates nesnesi bulunamadı");

            var allRates = ratesToken.ToObject<Dictionary<string, decimal>>() ?? new Dictionary<string, decimal>();

            return new ExchangeResultModelView
            {
                BaseCurrency = (string?)obj["base_code"] ?? "USD",
                Rates = allRates
            };
        }
        #endregion

        #region HAVA DURUMU
        private static readonly Dictionary<string, string> _weatherTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Sunny", "Güneşli" }, { "Partly cloudy", "Parçalı Bulutlu" }, { "Cloudy", "Bulutlu" },
            { "Overcast", "Kapalı" }, { "Mist", "Sisli" }, { "Patchy rain possible", "Hafif Yağmur İhtimali" }
        };

        private static string TranslateCondition(string eng) =>
            string.IsNullOrWhiteSpace(eng) ? eng : (_weatherTranslations.TryGetValue(eng, out var tr) ? tr : eng);

        public async Task<WeatherViewModel> GetWeatherAsync(string city)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://{WeatherHost}/weather/{city.ToLowerInvariant()}");
            req.Headers.Add("x-rapidapi-key", ApiKey);
            req.Headers.Add("x-rapidapi-host", WeatherHost);

            using var res = await _httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync();
            var obj = JObject.Parse(json);

            var rawText = (string?)obj["current"]?["condition"]?["text"] ?? "";

            return new WeatherViewModel
            {
                City = (string?)obj["location"]?["name"] ?? city,
                Region = (string?)obj["location"]?["region"] ?? "",
                Country = (string?)obj["location"]?["country"] ?? "",
                TempC = (double?)obj["current"]?["temp_c"] ?? 0,
                ConditionText = TranslateCondition(rawText),
                ConditionIcon = "https:" + ((string?)obj["current"]?["condition"]?["icon"] ?? "")
            };
        }

        public Task<WeatherViewModel> GetIstanbulWeatherAsync() => GetWeatherAsync("istanbul");
        public Task<WeatherViewModel> GetAnkaraWeatherAsync() => GetWeatherAsync("ankara");
        #endregion

        #region BASKETBOL
        public async Task<List<SportsScoreViewModel>> GetSportsScoresAsync(DateTime? dateUtc = null)
        {
            var dateParam = (dateUtc ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
            var uri = $"https://{BasketballHost}/games?date={dateParam}";

            var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Add("x-rapidapi-key", ApiKey);
            req.Headers.Add("x-rapidapi-host", BasketballHost);

            using var res = await _httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync();
            var arr = JObject.Parse(json)["response"] as JArray ?? new JArray();

            var list = new List<SportsScoreViewModel>();
            foreach (var item in arr)
            {
                var ts = (long?)item["timestamp"] ?? 0L;
                var date = DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;

                var homeScores = item["scores"]?["home"];
                var awayScores = item["scores"]?["away"];
                var league = item["league"];
                var country = item["country"];

                list.Add(new SportsScoreViewModel
                {
                    GameId = (int?)item["id"] ?? 0,
                    Date = date,
                    StatusShort = (string?)item["status"]?["short"] ?? "",
                    StatusLong = (string?)item["status"]?["long"] ?? "",
                    LeagueName = (string?)league?["name"] ?? "",
                    LeagueLogo = (string?)league?["logo"] ?? "",
                    CountryName = (string?)country?["name"] ?? "",
                    CountryFlag = (string?)country?["flag"] ?? "",
                    HomeTeam = (string?)item["teams"]?["home"]?["name"] ?? "",
                    HomeLogo = (string?)item["teams"]?["home"]?["logo"] ?? "",
                    HomeQ1 = (int?)homeScores?["quarter_1"] ?? 0,
                    HomeQ2 = (int?)homeScores?["quarter_2"] ?? 0,
                    HomeQ3 = (int?)homeScores?["quarter_3"] ?? 0,
                    HomeQ4 = (int?)homeScores?["quarter_4"] ?? 0,
                    HomeOvertime = (int?)homeScores?["over_time"],
                    HomeTotal = (int?)homeScores?["total"] ?? 0,
                    AwayTeam = (string?)item["teams"]?["away"]?["name"] ?? "",
                    AwayLogo = (string?)item["teams"]?["away"]?["logo"] ?? "",
                    AwayQ1 = (int?)awayScores?["quarter_1"] ?? 0,
                    AwayQ2 = (int?)awayScores?["quarter_2"] ?? 0,
                    AwayQ3 = (int?)awayScores?["quarter_3"] ?? 0,
                    AwayQ4 = (int?)awayScores?["quarter_4"] ?? 0,
                    AwayOvertime = (int?)awayScores?["over_time"],
                    AwayTotal = (int?)awayScores?["total"] ?? 0,
                });
            }
            return list;
        }
        #endregion
    }
}
