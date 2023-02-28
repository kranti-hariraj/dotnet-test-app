using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true);
    var configuration = builder.Build();

    var context = new DataContext(configuration);
    var countries = context.Countries.Select(x => x.Name).ToArray();
    return countries;
});


app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


[Table("Country")]
public class Country
{
    [Key]
    public string Name { get; set; }

    public string Region { get; set; }
}

public class DataContext : DbContext
{
    protected readonly IConfiguration Configuration;

    public DataContext(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // connect to sql server with connection string from app settings
        var conString = DecryptAndGetConnectionString("TestDbConnection");
        options.UseSqlServer(conString);
    }

    public DbSet<Country> Countries { get; set; }



    /* The actuial logic starts here */
    private const string ENCRYPTION_KEY = "base64password";
    private const string PASSWORD_KEY = "password";
    private const string CONNECTION_STRING_PATTERN = "(?<Key>[^=;]+)=(?<Val>[^;]+)";

    private string? DecryptAndGetConnectionString(string connectionName)
    {
        var rawConnectionString = Configuration.GetConnectionString(connectionName);
        if(rawConnectionString == null)
        {
            return null;
        }

        // Split the code into it's parts to create a dictionary to work with.
        var conDictionary = Regex.Matches(rawConnectionString, @CONNECTION_STRING_PATTERN)
            .Cast<Match>().ToDictionary(x => x.Groups[1].ToString().Trim().ToLower(), x => x.Groups[2].ToString().Trim());

        // If excryption enabled, then decrypt and remove the custom key.
        if (conDictionary.ContainsKey(ENCRYPTION_KEY))
        {
            if (Convert.ToBoolean(conDictionary[ENCRYPTION_KEY]))
            {
                var e_pwd = conDictionary[PASSWORD_KEY];
                var d_pwd = Encoding.UTF8.GetString(Convert.FromBase64String(e_pwd));
                conDictionary[PASSWORD_KEY] = d_pwd;
            }
            conDictionary.Remove(ENCRYPTION_KEY);
        }

        return string.Join(";", conDictionary.Select(x => x.Key + "=" + x.Value).ToArray());
    }
}