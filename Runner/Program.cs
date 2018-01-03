﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Spider;
using Spider.ArachneeCore;
using Spider.Exports;
using Spider.Serialization;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var spiderFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Arachnee.Spider");
            var now = DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss");
            var logFilePath = Path.Combine(spiderFolder, now + "_spider.log");
            Logger.Initialize(logFilePath);

            Console.WriteLine("Log file at " + logFilePath);

            if (!Directory.Exists(spiderFolder))
            {
                Directory.CreateDirectory(spiderFolder);
            }

            var downloader = new ArchiveDownloader();

            var moviesZipPath = downloader.DownloadMovies(DateTime.UtcNow.AddDays(-2), spiderFolder);

            var movieIdsPath = Unzipper.Unzip(moviesZipPath);
            
            var proxy = new TmdbProxy();

            var reader = new ArchiveReader(proxy);
            int entriesCount = reader.CountLines(movieIdsPath);
            var entries = reader.ReadMovies(movieIdsPath);

            string outputFilePath = Path.Combine(spiderFolder, now + "_output.spdr");
            
            var serializer = new HighPressureSerializer(outputFilePath);

            int i = 0;
            var chrono = Stopwatch.StartNew();
            foreach (var entry in entries)
            {
                i++;
                if (i % 25 == 0)
                {
                    float progress = (float)i / entriesCount * 100;
                    Logger.Instance.LogMessage($"{i}/{entriesCount} ({progress:##0.000}%) - elapsed: {chrono.Elapsed}");
                }
                
                var connectionsToCompress = new List<Connection>();
                foreach (var connection in entry.Connections)
                {
                    Logger.Instance.LogDebug(entry + " :: " + connection.Label + " :: " + connection.ConnectedId);
                    connectionsToCompress.Add(connection);
                }

                if (connectionsToCompress.Count == 0)
                {
                    continue;
                }

                serializer.CompressAndWrite(entry.Id, connectionsToCompress);
            }

            Console.WriteLine("Job done, press any key");
            Console.ReadKey();
        }
    }
}