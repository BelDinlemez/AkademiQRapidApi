namespace AkademiQRapidApi.Models
{
    public class DashboardViewModel
    {
        public ExchangeResultModelView? CurrencyRates { get; set; }
        public GasPriceViewModel? FuelPrices { get; set; }
        public MovieRecommendationViewModel? MovieOfTheDay { get; set; }

        public WeatherViewModel? IstanbulWeather { get; set; }
        public WeatherViewModel? AnkaraWeather { get; set; }

        public List<SportsScoreViewModel>? SportsScores { get; set; }
        public List<SportsScoreViewModel>? BasketballScores { get; set; } 

        public List<VolleyballMatchViewModel> VolleyballMatches { get; set; } = new();
        public List<NewsItemViewModel> TurkishNews { get; set; } = new();
        public CryptoQuoteViewModel CryptoQuote { get; set; } = new();
        public List<PricePointViewModel> CryptoSeries { get; set; } = new();



    }
}

public class NewsItemViewModel
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Url { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public DateTime? PublishedAt { get; set; }
    public string Source { get; set; } = "";
}

public class VolleyballMatchViewModel
{
    public string Country { get; set; }
    public string League { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }

    public string HomeLogo { get; set; }
    public string AwayLogo { get; set; }
    public string LeagueLogo { get; set; }

    public string Date { get; set; }
}


public class CryptoQuoteViewModel
{
    public string Symbol { get; set; } = "BTC";
    public decimal Price { get; set; }
    public decimal Change24h { get; set; } // %
}

public class PricePointViewModel
{
    public DateTime Time { get; set; }
    public decimal Price { get; set; }
}

public class ExchangeResultModelView
    {
        public string? BaseCurrency { get; set; }
        public Dictionary<string, decimal>? Rates { get; set; }
    }

    public class GasPriceViewModel
    {
        public decimal Gasoline { get; set; }
        public decimal Diesel { get; set; }
    }

public class MovieRecommendationViewModel
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string Url { get; set; } = "";
}

public class WeatherViewModel
{
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public double?  TempC { get; set; }
        public string? ConditionText { get; set; }
        public string? ConditionIcon { get; set; }
    }

public class SportsScoreViewModel
{// Genel maç bilgileri
    public int GameId { get; set; }
    public DateTime Date { get; set; }
    public string StatusShort { get; set; } = string.Empty;
    public string StatusLong { get; set; } = string.Empty;

    public string LeagueName { get; set; } = string.Empty;
    public string LeagueLogo { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string CountryFlag { get; set; } = string.Empty;

    // Ev sahibi takıma ait skorlar
    public int HomeQ1 { get; set; }
    public int HomeQ2 { get; set; }
    public int HomeQ3 { get; set; }
    public int HomeQ4 { get; set; }
    public int? HomeOvertime { get; set; }
    public int HomeTotal { get; set; }
    public string HomeTeam { get; set; } = string.Empty;
    public string HomeLogo { get; set; } = string.Empty;

    // Deplasman takıma ait skorlar
    public int AwayQ1 { get; set; }
    public int AwayQ2 { get; set; }
    public int AwayQ3 { get; set; }
    public int AwayQ4 { get; set; }
    public int? AwayOvertime { get; set; }
    public int AwayTotal { get; set; }
    public string AwayTeam { get; set; } = string.Empty;
    public string AwayLogo { get; set; } = string.Empty;
}
