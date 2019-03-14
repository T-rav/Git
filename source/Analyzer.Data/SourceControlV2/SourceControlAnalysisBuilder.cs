﻿using System;
using System.Collections.Generic;
using System.Linq;
using Analyzer.Data.Developer;
using Analyzer.Domain.Reporting;
using Analyzer.Domain.SourceControl;
using LibGit2Sharp;
using Branch = Analyzer.Domain.SourceControl.Branch;
using ISourceControlAnalysis = Analyzer.Domain.SourceControlV2.ISourceControlAnalysis;
using ISourceControlAnalysisBuilder = Analyzer.Domain.SourceControlV2.ISourceControlAnalysisBuilder;

namespace Analyzer.Data.SourceControlV2
{
    public class SourceControlAnalysisBuilder : ISourceControlAnalysisBuilder
    {
        private string _repoPath;
        private DateTime _start;
        private DateTime _end;
        private int _workWeekHours;
        private double _workingDaysPerWeek;
        private string _branch;
        private bool _isEntireHistory;
        private readonly List<string> _ignorePatterns;
        private readonly List<DayOfWeek> _weekends;
        private bool _ignoreComments;
        private string _aliasMapping;

        public SourceControlAnalysisBuilder()
        {
            _workWeekHours = 40;
            _workingDaysPerWeek = 5;
            _branch = Branches.Master.Value;
            _ignorePatterns = new List<string>();
            _weekends = new List<DayOfWeek>();
            _aliasMapping = string.Empty;
        }

        public ISourceControlAnalysisBuilder WithPath(string repoPath)
        {
            _repoPath = repoPath;
            return this;
        }

        public ISourceControlAnalysisBuilder WithRange(DateTime start, DateTime end)
        {
            _start = start;
            _end = end;
            _isEntireHistory = false;
            return this;
        }

        public ISourceControlAnalysisBuilder WithWorkingWeekHours(int workWeekHours)
        {
            _workWeekHours = workWeekHours;
            return this;
        }

        public ISourceControlAnalysisBuilder WithWorkingDaysPerWeek(double workingDaysPerWeek)
        {
            _workingDaysPerWeek = workingDaysPerWeek;
            return this;
        }

        public ISourceControlAnalysisBuilder WithBranch(string branch)
        {
            _branch = branch;
            return this;
        }

        public ISourceControlAnalysisBuilder WithIgnorePatterns(IEnumerable<string> patterns)
        {
            if (patterns == null)
            {
                return this;
            }

            _ignorePatterns.AddRange(patterns);
            return this;
        }

        public ISourceControlAnalysisBuilder WithEntireHistory()
        {
            _isEntireHistory = true;
            return this;
        }

        public ISourceControlAnalysisBuilder WithWeekends(IEnumerable<DayOfWeek> days)
        {
            _weekends.AddRange(days);
            return this;
        }

        public ISourceControlAnalysisBuilder WithIgnoreComments(bool ignoreComments)
        {
            _ignoreComments = ignoreComments;
            return this;
        }

        public ISourceControlAnalysisBuilder WithAliasMapping(string aliasMapping)
        {
            _aliasMapping = aliasMapping;
            return this;
        }

        public ISourceControlAnalysis Build()
        {
            if (NotValidGitRepository(_repoPath))
            {
                throw new Exception($"Invalid path [{_repoPath}]");
            }

            var repository = new Repository(_repoPath);
            if (InvalidBranchName(repository))
            {
                throw new Exception($"Invalid branch [{_branch}]");
            }

            var reportRange = new ReportingPeriod { Start = _start, End = _end, HoursPerWeek = _workWeekHours, DaysPerWeek = _workingDaysPerWeek, Weekends = _weekends };

            if (_isEntireHistory)
            {
                MakeRangeEntireHistory(repository, reportRange);
            }

            var aliasMapping = new Aliases(_aliasMapping);
            var context = CreateSourceControlContext(reportRange);
            return new SourceControlAnalysis(repository, aliasMapping, context);
        }

        private AnalysisContext CreateSourceControlContext(ReportingPeriod reportRange)
        {
            var context = new AnalysisContext
            {
                ReportRange = reportRange,
                Branch = Branch.Create(_branch),
                IgnorePatterns = _ignorePatterns,
                IgnoreComments = _ignoreComments
            };
            return context;
        }

        private void MakeRangeEntireHistory(Repository repository, ReportingPeriod reportRange)
        {
            var commits = GetCommitsForSelectedBranch(repository);

            reportRange.Start = GetFirstCommit(commits);
            reportRange.End = GetLastCommit(commits);
        }

        private ICommitLog GetCommitsForSelectedBranch(Repository repository)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = repository.Branches[_branch]
            };

            if (Branches.MasterNotSelected(_branch))
            {
                filter.ExcludeReachableFrom = repository.Branches[Branches.Master.Value];
            }

            var commitLog = repository.Commits.QueryBy(filter);
            return commitLog;
        }

        private static DateTime GetLastCommit(ICommitLog commitLog)
        {
            return commitLog.First().Author.When.Date;
        }

        private static DateTime GetFirstCommit(ICommitLog commitLog)
        {
            return commitLog.Last().Author.When.Date;
        }

        private bool InvalidBranchName(Repository repository)
        {
            return repository.Branches[_branch] == null;
        }

        private bool NotValidGitRepository(string repository)
        {
            return !Repository.IsValid(repository);
        }
    }
}