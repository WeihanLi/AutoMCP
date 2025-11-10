using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.ComponentModel;

namespace AutoMcp.Example.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private static readonly IQueryable<WeatherForecast> _repository = Enumerable.Range(-365, 365 * 3).Select(index => new WeatherForecast
    {
        Date = DateOnly.FromDateTime(DateTime.Today.AddDays(index)),
        TemperatureC = Random.Shared.Next(-20, 55),
        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
    })
        .ToArray()
        .AsQueryable();

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    [Description("Get the weather forecast for the given date.")]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public ActionResult<WeatherForecast> Get(DateOnly date)
    {
        var forecast = new WeatherForecast
        {
            Date = date,
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        };

        return Ok(forecast);
    }

    [HttpGet(Name = "GetMultipleWeatherForecasts")]
    [Description("Get multiple weather forecasts for the next number of days.")]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    public ActionResult<IEnumerable<WeatherForecast>> GetMultiple(ODataQueryOptions<WeatherForecast> options)
    {
        return Ok(options.ApplyTo(_repository));
    }
}
