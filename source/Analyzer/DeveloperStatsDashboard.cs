﻿using System;
using System.Collections.Generic;
using System.Linq;
using Analyzer.Domain;

namespace Analyzer
{
    public class DeveloperStatsDashboard
    {
        public static string Version => "0.9.2";
        public static ConsoleColor DefaultColor { get; private set; }

        public DeveloperStatsDashboard()
        {
            DefaultColor = Console.ForegroundColor;
        }

        public void RenderDashboard(ISourceControlRepository repo)
        {
            var authors = repo.List_Authors();
            var stats = repo.Build_Individual_Developer_Stats(authors);

            PrintApplicationHeader(repo.ReportingRange.Start, repo.ReportingRange.End);
            PrintDeveloperStatsTableHeader();
            PrintDeveloperStatsTable(stats);
            PrintDeveloperAverages(stats, repo.ReportingRange.Period_Days());
        }

        private void PrintApplicationHeader(DateTime start, DateTime end)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"GD3 Stats - v{Version}");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"For period {start:yyyy-MM-dd} - {end:yyyy-MM-dd}");
            Console.ForegroundColor = DefaultColor;
        }
        
        private void PrintDeveloperStatsTableHeader()
        {
            PrintDashedLine();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Individual Developer Stats");
            Console.ForegroundColor = DefaultColor;
            PrintDashedLine();
            Console.WriteLine("Developer               | Period Active Days | Active Days Per Week | Commits / Day | Lines of Change Per Hour | Impact | Risk Factor | Lines Added | Lines Removed | Churn | Rtt100 | Ptt100 | Dtt100");
            PrintDashedLine();
        }

        private void PrintDeveloperAverages(List<DeveloperStats> stats, int totalPeriodDays)
        {
            var totalDevelopers = stats.Count*1.0;

            var periodActiveDays = Math.Round(stats.Sum(x => x.PeriodActiveDays) / totalDevelopers,2);
            var activeDays = Math.Round(stats.Sum(x => x.ActiveDaysPerWeek) / totalDevelopers,2);
            var commitsPerDay = Math.Round(stats.Sum(x => x.CommitsPerDay) / totalDevelopers,2);
            var linesOfChange = Math.Round(stats.Sum(x => x.LinesOfChangePerHour) / totalDevelopers,2);
            var impact = Math.Round(stats.Sum(x => x.Impact) / totalDevelopers,2);
            var riskFactor = Math.Round(stats.Sum(x => x.RiskFactor) / totalDevelopers,2);
            var linesAdded = Math.Round(stats.Sum(x => x.LinesAdded) / totalDevelopers,2);
            var linesRemoved = Math.Round(stats.Sum(x => x.LinesRemoved) / totalDevelopers,2);
            var churn = Math.Round(stats.Sum(x => x.Churn) / totalDevelopers,2);
            var rtt100 = Math.Round(stats.Sum(x => x.Rtt100) / totalDevelopers,2);
            var ptt100 = Math.Round(stats.Sum(x => x.Ptt100) / totalDevelopers,2);
            var dtt100 = Math.Round(stats.Sum(x => x.Dtt100) / totalDevelopers,2);

            var renderedLine = $"{PaddedPrint("Averages",26)}" +
                               $"{PaddedPrint($"{periodActiveDays} of {totalPeriodDays}", 21)}" +
                               $"{PaddedPrint($"{activeDays}*",23)}" +
                               $"{PaddedPrint(commitsPerDay,16)}" +
                               $"{PaddedPrint(linesOfChange, 27)}" +
                               $"{PaddedPrint($"{impact}^", 9)}" +
                               $"{PaddedPrint(riskFactor, 14)}" +
                               $"{PaddedPrint(linesAdded, 14)}" +
                               $"{PaddedPrint(linesRemoved, 16)}" +
                               $"{PaddedPrint(churn, 8)}" +
                               $"{PaddedPrint(rtt100, 9)}" +
                               $"{PaddedPrint(ptt100, 9)}" +
                               $"{PaddedPrint(dtt100, 0)}";

            Console.WriteLine(renderedLine);
            PrintDashedLine();
            Console.WriteLine("* Global average is 3.2 days per week");
            Console.WriteLine("^ Approximation of congative load carried when contributing.");
            PrintDashedLine();
        }

        private void PrintDeveloperStatsTable(List<DeveloperStats> stats)
        {
            var orderedStats = stats.OrderByDescending(x => x.PeriodActiveDays);
            foreach (var stat in orderedStats)
            {
                Console.WriteLine(stat);
            }
            PrintDashedLine();
        }

        private void PrintDashedLine()
        {
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");
        }

        private string PaddedPrint(object value, int fieldWidth)
        {
            return value.ToString().PadRight(fieldWidth, ' ');
        }
    }
}
