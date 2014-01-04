using System.IO;
using System.Linq;
using System.Management.Automation;
using Illallangi.FlightLog.Model;
using Newtonsoft.Json;

namespace Illallangi.FlightLog.PowerShell
{
    [Cmdlet(VerbsData.Export, "FlightLogToJson")]
    public sealed class ExportFlightLogToJsonCmdlet : NinjectCmdlet
    {
        [Parameter(Mandatory = true)]
        public string FileName { get; set; }

        protected override void BeginProcessing()
        {
            File.WriteAllText(
                Path.GetFullPath(this.FileName),
                JsonConvert.SerializeObject(
                    new
                    {
                        Airports = this.Get<ISource<Airport>>()
                            .Retrieve()
                            .Select(
                                airport => new
                                {
                                    airport.Icao,
                                    airport.Iata,
                                    airport.Name,
                                    airport.City,
                                    airport.Country,
                                    airport.Latitude,
                                    airport.Longitude,
                                    airport.Altitude,
                                    airport.Timezone,
                                }),
                        Years = this.Get<ISource<Year>>()
                            .Retrieve()
                            .Select(
                                year => new
                                {
                                    Title = year.Name,
                                    Trips = this.Get<ISource<Trip>>()
                                        .Retrieve(new Trip { Year = year.Name })
                                        .Select(
                                            trip => new
                                            {
                                                Title = trip.Name,
                                                trip.Description,
                                                Flights = this.Get<ISource<Flight>>()
                                                    .Retrieve(new Flight { Year = year.Name, Trip = trip.Name })
                                                    .OrderBy(flight => flight.Departure)
                                                    .Select(
                                                        flight => new
                                                        {
                                                            flight.Departure,
                                                            flight.Arrival,
                                                            flight.Airline,
                                                            flight.Number,
                                                            flight.Origin,
                                                            flight.Destination,
                                                            flight.Aircraft,
                                                            flight.Seat,
                                                            flight.Note,
                                                        })
                                            })
                                })
                    },
                    Formatting.Indented,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }
    }
}