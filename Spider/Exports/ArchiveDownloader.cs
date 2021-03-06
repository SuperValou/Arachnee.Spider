﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using RestSharp;
using Spider.ArachneeCore;
using Spider.Tmdb;

namespace Spider.Exports
{
    public class ArchiveDownloader
    {
        private readonly RestClient _client = new RestClient("http://files.tmdb.org/p/exports/");

        public string Download<TEntry>(DateTime archiveDate, string destinationFolder) where TEntry : Entry
        {
            if (archiveDate.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(nameof(archiveDate), $"{nameof(archiveDate)} is not UTC.");
            }

            if (archiveDate > DateTime.UtcNow)
            {
                throw new ArgumentException(nameof(archiveDate), $"{nameof(archiveDate)} is not valid because it is in the future.");
            }

            string entryResourceName;
            switch (typeof(TEntry).Name)
            {
                case nameof(Movie):
                    entryResourceName = "movies";
                    break;
                case nameof(Artist):
                    entryResourceName = "person";
                    break;
                case nameof(TvSeries):
                    entryResourceName = "tv_series";
                    break;
                default:
                    string message = "Unhandled type " + typeof(TEntry).Name;
                    Logger.Instance.LogError(message);
                    throw new ArgumentException(message);
            }

            string resource = $"{entryResourceName}_ids_{archiveDate.Month:00}_{archiveDate.Day:00}_{archiveDate.Year:0000}.json.gz";

            return Download(destinationFolder, resource);
        }
        
        private string Download(string destinationFolder, string resource)
        {
            if (!Directory.Exists(destinationFolder))
            {
                throw new DirectoryNotFoundException($"Folder doesn't exist at \"{destinationFolder}\"");
            }

            var request = new RestRequest(resource, Method.GET);

            Logger.Instance.LogMessage("Downloading " + resource + "...");

            var response = _client.Execute(request);

            Logger.Instance.LogMessage("Downloading done.");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new FailedRequestException(_client.BuildUri(request).ToString(), response);
            }

            string filePath = Path.Combine(destinationFolder, resource);

            File.WriteAllBytes(filePath, response.RawBytes);

            Logger.Instance.LogMessage("Archive file created at " + filePath);

            return filePath;
        }
    }
}