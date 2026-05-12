using System;
using System.Collections.Generic;

namespace LegendBar.Helpers
{
    public class WeatherData
    {
        // Current conditions
        public double Temperature { get; set; }
        public double ApparentTemperature { get; set; }
        public int WeatherCode { get; set; }
        public double Humidity { get; set; }
        public double WindSpeed { get; set; }
        public double WindDirection { get; set; }
        public double Precipitation { get; set; }
        public double CloudCover { get; set; }
        public double Visibility { get; set; }
        public double UvIndex { get; set; }
        public bool IsDay { get; set; }

        // Location
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string CityName { get; set; } = "";

        // Daily forecast — 7 days
        public List<WeatherDayForecast> Forecast { get; set; } = new();

        // Metadata
        public DateTime FetchedAt { get; set; }
    }

    public class WeatherDayForecast
    {
        public DateTime Date { get; set; }
        public int WeatherCode { get; set; }
        public double TempMax { get; set; }
        public double TempMin { get; set; }
        public string Sunrise { get; set; } = "";
        public string Sunset { get; set; } = "";
        public double PrecipitationSum { get; set; }
        public double UvIndexMax { get; set; }
    }
}