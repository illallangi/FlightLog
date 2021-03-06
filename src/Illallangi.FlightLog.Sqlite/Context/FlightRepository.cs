using System;
using System.Collections.Generic;
using System.Linq;
using Illallangi.LiteOrm;

namespace Illallangi.FlightLog.Sqlite.Context
{
    using System.Data.SQLite;

    using Common.Logging;

    using Illallangi.FlightLog.Config;

    public class FlightRepository : FlightLogRepositoryBase<IFlight>
    {
        #region Fields

        private readonly IRepository<ITrip> currentTripRepository;
        
        private readonly IRepository<IAirport> currentAirportRepository;

        #endregion

        #region Constructor

        public FlightRepository(
            IFlightLogConfig flightLogConfig, 
            IRepository<ITrip> tripRepository,
            IRepository<IAirport> airportRepository,
            ILog log)
        : base(
            flightLogConfig,
            log)
        {
            this.Log.DebugFormat(
                @"FlightRepository(""{0}"", ""{1}"", ""{2}"", ""{3}"")",
                flightLogConfig,
                tripRepository,
                airportRepository,
                log);
            this.currentTripRepository = tripRepository;
            this.currentAirportRepository = airportRepository;
        }

        #endregion

        #region Properties

        private IRepository<ITrip> TripRepository
        {
            get { return this.currentTripRepository; }
        }

        private IRepository<IAirport> AirportRepository
        {
            get { return this.currentAirportRepository; }
        }

        #endregion

        #region Methods

        protected override int Import(SQLiteConnection cx, SQLiteTransaction tx, params IFlight[] objs)
        {
            var trips = objs.Select(c => new Tuple<string, string>(c.Year, c.Trip))
                             .Distinct()
                             .ToDictionary(trip => trip, trip => this.TripRepository.Retrieve(new Trip { Year = trip.Item1, Name = trip.Item2 }).Single().Id.Value);

            var airports = objs.SelectMany(c => new[] { c.Origin, c.Destination })
                                .Distinct()
                                .ToDictionary(airport => airport, airport => this.AirportRepository.Retrieve(new Airport { Icao = airport }).Single().Id.Value);

            foreach (var obj in objs)
            {
                this.Log.DebugFormat(@"FlightRepository.Create(""{0}"")", obj);
                try
                {
                    cx.InsertInto("Flight")
                    .Values("TripId", trips[new Tuple<string, string>(obj.Year, obj.Trip)])
                    .Values("OriginId", airports[obj.Origin])
                    .Values("DestinationId", airports[obj.Destination])
                    .Values("Departure", obj.Departure)
                    .Values("Arrival", obj.Arrival)
                    .Values("Airline", obj.Airline)
                    .Values("Number", obj.Number)
                    .Values("Aircraft", obj.Aircraft)
                    .Values("Seat", obj.Seat)
                    .Values("Note", obj.Note)
                    .Go(tx);
                }
                catch (SQLiteException sqe)
                {
                    throw new RepositoryException<IFlight>(obj, sqe.Message, sqe.ErrorCode, sqe);
                }
            }

            return objs.Count();
        }

        public override IEnumerable<IFlight> Retrieve(IFlight obj = null)
        {
            this.Log.DebugFormat(@"FlightRepository.Retrieve(""{0}"")", obj);

            return this.GetConnection()
                .Select<Flight>("Flights")
                .Column("FlightId", (flight, i) => flight.Id = i, null == obj ? null : obj.Id)
                .Column("YearName", (flight, s) => flight.Year = s, null == obj ? null : obj.Year)
                .Column("TripName", (flight, s) => flight.Trip = s, null == obj ? null : obj.Trip)
                .Column("OriginIcao", (flight, s) => flight.Origin = s, null == obj ? null : obj.Origin)
                .Column("OriginTimezone", (flight, s) => flight.OriginTimezone = s)
                .FloatColumn("OriginLatitude", (flight, s) => flight.OriginLatitude = s)
                .FloatColumn("OriginLongitude", (flight, s) => flight.OriginLongitude = s)
                .Column("DestinationIcao", (flight, s) => flight.Destination = s, null == obj ? null : obj.Destination)
                .Column("DestinationTimezone", (flight, s) => flight.DestinationTimezone = s)
                .FloatColumn("DestinationLatitude", (flight, s) => flight.DestinationLatitude = s)
                .FloatColumn("DestinationLongitude", (flight, s) => flight.DestinationLongitude = s)
                .Column("Departure", (flight, s) => flight.Departure = s)
                .Column("Arrival", (flight, s) => flight.Arrival = s)
                .Column("Airline", (flight, s) => flight.Airline = s, null == obj ? null : obj.Airline)
                .Column("Number", (flight, s) => flight.Number = s, null == obj ? null : obj.Number)
                .Column("Aircraft", (flight, s) => flight.Aircraft = s)
                .Column("Seat", (flight, s) => flight.Seat = s)
                .Column("Note", (flight, s) => flight.Note = s)
                .Go();
        }

        public override IFlight Update(IFlight obj)
        {
            this.Log.DebugFormat(@"FlightRepository.Update(""{0}"")", obj);

            throw new NotImplementedException();
        }

        public override void Delete(params IFlight[] objs)
        {
            foreach (var obj in objs)
            {
                this.Log.DebugFormat(@"FlightRepository.Delete(""{0}"")", obj);

                this.GetConnection()
                    .DeleteFrom("Flight")
                    .Where("FlightId", obj.Id)
                    .Where("Departure", obj.Departure)
                    .Where("Arrival", obj.Arrival)
                    .Where("Airline", obj.Airline)
                    .Where("Number", obj.Number)
                    .Where("Aircraft", obj.Aircraft)
                    .Where("Seat", obj.Seat)
                    .Where("Note", obj.Note)
                    .CreateCommand()
                    .ExecuteNonQuery();
            }
        }

        #endregion
    }
}