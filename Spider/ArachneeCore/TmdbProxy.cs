﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Spider.Tmdb;
using Spider.Tmdb.TmdbObjects;

namespace Spider.ArachneeCore
{
    public class TmdbProxy
    {
        private const char IdSeparator = '-';

        private readonly TmdbClient _client = new TmdbClient();

        private readonly Dictionary<string, ConnectionType> _handledCrewJobs = new Dictionary<string, ConnectionType>
        {
            {"Director", ConnectionType.Director},
            {"Boom Operator", ConnectionType.BoomOperator},
        };
        
        public Entry GetMovie(ulong id)
        {
            return GetEntry(nameof(Movie) + IdSeparator + id);
        }

        public Entry GetArtist(ulong id)
        {
            return GetEntry(nameof(Artist) + IdSeparator + id);
        }

        /// <summary>
        /// Returns the <see cref="Entry"/> corresponding to the given id.
        /// </summary>
        public Entry GetEntry(string entryId)
        {
            if (string.IsNullOrEmpty(entryId))
            {
                throw new ArgumentException($"\"{entryId}\" is not a valid id.", nameof(entryId));
            }

            var split = entryId.Split(IdSeparator);

            if (split.Length != 2)
            {
                throw new ArgumentException($"\"{entryId}\" is not a valid id.", nameof(entryId));
            }

            var entryType = split[0];
            var tmdbId = split[1];

            if (string.IsNullOrEmpty(entryType) || string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentException($"\"{entryId}\" is not a valid id.", nameof(entryId));
            }

            ulong id;
            if (!ulong.TryParse(tmdbId, out id))
            {
                throw new ArgumentException($"\"{entryId}\" is not a valid id.", nameof(entryId));
            }

            // convert the tmdb object to its corresponding Entry
            Entry entry;
            switch (entryType)
            {
                case nameof(Movie):
                    var tmdbMovie = _client.GetMovie(id);
                    entry = ConvertToMovie(tmdbMovie);
                    break;

                case nameof(Artist):
                    var tmdbPerson = _client.GetPerson(id);
                    entry = ConvertToArtist(tmdbPerson);
                    break;

                case nameof(TvSeries):
                    var tmdbTvSeries = _client.GetTvSeries(id);
                    entry = ConvertToTvSeries(tmdbTvSeries);
                    break;

                default:
                    throw new ArgumentException(
                        $"\"{entryId}\" cannot be processed because \"{entryType}\" is not a handled entry type.",
                        nameof(entryId));
            }

            return entry;
        }

        private Artist ConvertToArtist(TmdbPerson tmdbPerson)
        {
            // create the Artist from the tmdbPerson
            var artist = JsonConvert.DeserializeObject<Artist>(
                JsonConvert.SerializeObject(tmdbPerson, TmdbJsonSettings.Instance), TmdbJsonSettings.Instance);
            artist.Id = nameof(Artist) + IdSeparator + artist.Id;
            artist.NickNames = tmdbPerson.AlsoKnownAs;

            // set images
            artist.MainImagePath = tmdbPerson.ProfilePath;

            // create the connections
            artist.Connections = new List<Connection>();

            foreach (var cast in tmdbPerson.CombinedCredits.Cast.Where(c => !string.IsNullOrEmpty(c.PosterPath)))
            {
                var id = cast.MediaType == "tv"
                    ? nameof(TvSeries) + IdSeparator + cast.Id
                    : nameof(Movie) + IdSeparator + cast.Id;

                artist.Connections.Add(new Connection
                {
                    ConnectedId = id,
                    Type = ConnectionType.Actor,
                    Label = cast.Character
                });
            }

            foreach (var cast in tmdbPerson.CombinedCredits.Crew.Where(c => !string.IsNullOrEmpty(c.PosterPath)))
            {
                ConnectionType type;
                if (_handledCrewJobs.TryGetValue(cast.Job, out type))
                {
                    artist.Connections.Add(new Connection
                    {
                        ConnectedId = nameof(Movie) + IdSeparator + cast.Id,
                        Type = type,
                        Label = cast.Job
                    });
                }
                else
                {
                    artist.Connections.Add(new Connection
                    {
                        ConnectedId = nameof(Movie) + IdSeparator + cast.Id,
                        Type = ConnectionType.Crew,
                        Label = cast.Job
                    });
                }
            }

            return artist;
        }

        private Movie ConvertToMovie(TmdbMovie tmdbMovie)
        {
            // clean up fields
            if (!tmdbMovie.Runtime.HasValue)
            {
                tmdbMovie.Runtime = 0;
            }

            // create the Movie from the tmdbMovie
            var movie = JsonConvert.DeserializeObject<Movie>(JsonConvert.SerializeObject(tmdbMovie));
            movie.Id = nameof(Movie) + IdSeparator + movie.Id;
            movie.Tags = tmdbMovie.Genres.Select(g => g.Name).ToList();

            // set images
            movie.MainImagePath = tmdbMovie.PosterPath;

            // create the connections
            movie.Connections = new List<Connection>();

            foreach (var cast in tmdbMovie.Credits.Cast.Where(c => !string.IsNullOrEmpty(c.ProfilePath)))
            {
                movie.Connections.Add(new Connection
                {
                    ConnectedId = nameof(Artist) + IdSeparator + cast.Id,
                    Type = ConnectionType.Actor,
                    Label = cast.Character
                });
            }

            foreach (var cast in tmdbMovie.Credits.Crew.Where(c => !string.IsNullOrEmpty(c.ProfilePath)))
            {
                ConnectionType type;
                if (_handledCrewJobs.TryGetValue(cast.Job, out type))
                {
                    movie.Connections.Add(new Connection
                    {
                        ConnectedId = nameof(Artist) + IdSeparator + cast.Id,
                        Type = type,
                        Label = cast.Job
                    });
                }
                else
                {
                    movie.Connections.Add(new Connection
                    {
                        ConnectedId = nameof(Artist) + IdSeparator + cast.Id,
                        Type = ConnectionType.Crew,
                        Label = cast.Job
                    });
                }
            }

            return movie;
        }

        private TvSeries ConvertToTvSeries(TmdbTvSeries tmdbTvSeries)
        {
            var tvSeries = JsonConvert.DeserializeObject<TvSeries>(
                JsonConvert.SerializeObject(tmdbTvSeries, TmdbJsonSettings.Instance), TmdbJsonSettings.Instance);

            tvSeries.Id = nameof(TvSeries) + IdSeparator + tmdbTvSeries.Id;
            tvSeries.MainImagePath = tmdbTvSeries.PosterPath;

            foreach (var cast in tmdbTvSeries.Credits.Cast.Where(c => !string.IsNullOrEmpty(c.ProfilePath)))
            {
                tvSeries.Connections.Add(new Connection
                {
                    ConnectedId = nameof(Artist) + IdSeparator + cast.Id,
                    Type = ConnectionType.Actor,
                    Label = cast.Character
                });
            }

            foreach (var cast in tmdbTvSeries.Credits.Crew.Where(c => !string.IsNullOrEmpty(c.ProfilePath)))
            {
                ConnectionType type;
                if (_handledCrewJobs.TryGetValue(cast.Job, out type))
                {
                    tvSeries.Connections.Add(new Connection
                    {
                        ConnectedId = nameof(Artist) + IdSeparator + cast.Id,
                        Type = type,
                        Label = cast.Job
                    });
                }
                else
                {
                    tvSeries.Connections.Add(new Connection
                    {
                        ConnectedId = nameof(Artist) + IdSeparator + cast.Id,
                        Type = ConnectionType.Crew,
                        Label = cast.Job
                    });
                }
            }

            var creators = tmdbTvSeries.CreatedBy.Select(creator => nameof(Artist) + IdSeparator + creator.Id.ToString());
            tvSeries.Connections.AddRange(creators.Select(creator => new Connection
            {
                ConnectedId = creator,
                Label = "Created by",
                Type = ConnectionType.CreatedBy
            }));
            
            return tvSeries;
        }
    }
}