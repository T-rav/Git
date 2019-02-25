﻿using Analyzer.Data.SourceControl;
using Analyzer.Data.Test.Utils;
using Analyzer.Domain.Developer;
using Analyzer.Domain.Team;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TddBuddy.System.Utils.JsonUtils;

namespace Analyzer.Data.Tests.SourceRepository
{
    [TestFixture]
    public class SourceControlAnalysisTests
    {
        [TestFixture]
        public class ListAuthors
        {
            [Test]
            public void WhenMaster_ShouldReturnAllActiveDevelopers()
            {
                // arrange
                var authorName = "T-rav";

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, "tmfrisinger@gmail.com");

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-07-16 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .Make_Commit(commit1)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                             .WithPath(context.Path)
                             .WithRange(DateTime.Parse("2018-7-16"), DateTime.Parse("2018-07-17"))
                             .Build();
                // act
                var actual = sut.List_Authors();
                // assert
                var expected = new List<Author>
                {
                    new Author{Name = "T-rav", Emails = new List<string>{"tmfrisinger@gmail.com"}}
                };
                actual.Should().BeEquivalentTo(expected);
            }

            [Test]
            public void WhenDeveloperBranch_ShouldReturnAllActiveDevelopers()
            {
                // arrange
                var branchName = "my-branch";
                var authorName = "Travis";
                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, "travis@frisinger.com");

                var commit1 = commitBuilder
                    .With_Branch(branchName)
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-07-16 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_Branch(branchName)
                    .With_Author("T-rav", "tmfrisinger@gmail.com")
                    .With_File_Name("file2.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-07-17 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .On_Branch(branchName)
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithBranch(branchName)
                    .WithRange(DateTime.Parse("2018-7-16"), DateTime.Parse("2018-07-17"))
                    .Build();
                // act
                var actual = sut.List_Authors();
                // assert
                var expected = new List<Author>
                {
                    new Author{Name = "T-rav", Emails = new List<string>{"tmfrisinger@gmail.com"}},
                    new Author{Name = "Travis", Emails = new List<string>{"travis@frisinger.com"}}
                };
                actual.Should().BeEquivalentTo(expected);
            }

            [Test]
            public void WhenUsingAlias_ShouldReturnSingleDeveloperWithTwoEmails()
            {
                // arrange
                var branchName = "my-branch";
                var authorName = "T-rav";
                var aliasMap = new List<Alias>
                {
                    new Alias {Name = authorName, Emails = new List<string> { "tmfrisinger@gmail.com", "travis@frisinger.com" } }
                };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, "tmfrisinger@gmail.com");

                var commit1 = commitBuilder
                    .With_Branch(branchName)
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-07-16 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_Author(authorName, "travis@frisinger.com")
                    .With_Branch(branchName)
                    .With_File_Name("file2.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-07-17 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .On_Branch(branchName)
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Build();

                using (var aliasFileContext = WriteAliasMapping(aliasMap))
                {
                    var sut = new SourceControlAnalysisBuilder()
                        .WithPath(context.Path)
                        .WithEntireHistory()
                        .WithBranch("my-branch")
                        .WithAliasMapping(aliasFileContext.Path)
                        .Build();
                    // act
                    var actual = sut.List_Authors();
                    // assert
                    var expected = 1;
                    actual.Count.Should().Be(expected);
                }
            }

            [Test]
            public void WhenNullAliases_ShouldReturnTwoDevelopers()
            {
                // arrange
                var branchName = "my-branch";
                var authorName = "T-rav";

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, "tmfrisinger@gmail.com");

                var commit1 = commitBuilder
                    .With_Branch(branchName)
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-07-16 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_Branch(branchName)
                    .With_Author("Travis", "travis@frisinger.com")
                    .With_File_Name("file2.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-07-17 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .On_Branch(branchName)
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithEntireHistory()
                    .WithBranch(branchName)
                    .Build();
                // act
                var actual = sut.List_Authors();
                // assert
                var expected = 2;
                actual.Count.Should().Be(expected);
            }

            [Test]
            public void WhenEmptyAliases_ShouldReturnTwoDevelopers()
            {
                // arrange
                var branchName = "my-branch";
                var authorName = "T-rav";
                var aliasMap = new List<Alias>();

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, "tmfrisinger@gmail.com");

                var commit1 = commitBuilder
                    .With_Branch(branchName)
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-07-16 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_Branch(branchName)
                    .With_Author("Travis", "travis@frisinger.com")
                    .With_File_Name("file2.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-07-17 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .On_Branch(branchName)
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Build();

                using (var aliasFileContext = WriteAliasMapping(aliasMap))
                {
                    var sut = new SourceControlAnalysisBuilder()
                        .WithPath(context.Path)
                        .WithEntireHistory()
                        .WithBranch(branchName)
                        .WithAliasMapping(aliasFileContext.Path)
                        .Build();
                    // act
                    var actual = sut.List_Authors();
                    // assert
                    var expected = 2;
                    actual.Count.Should().Be(expected);
                }
            }
        }

        [TestFixture]
        public class ListPeriodActivity
        {
            [TestCase("2018-09-10", "2018-09-14", 3)]
            [TestCase("2018-09-12", "2018-09-12", 1)]
            public void WhenMaster_ShouldReturnActiveDays(DateTime start, DateTime end, int expected)
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file2.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-12 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-14 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithRange(start, end)
                    .WithPath(context.Path)
                    .Build();
                // act
                var actual = sut.Period_Active_Days(author);
                // assert
                actual.Should().Be(expected);
            }

            [TestCase("2018-09-10", "2018-09-14", 3)]
            [TestCase("2018-09-12", "2018-09-12", 1)]
            public void WhenAliases_ShouldReturnActiveDays(DateTime start, DateTime end, int expected)
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var email2 = "travis@frisinger.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email, email2 } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_Author(authorName, email2)
                    .With_File_Name("file2.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-12 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_Author(authorName, email)
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-14 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithRange(start, end)
                    .WithPath(context.Path)
                    .Build();
                
                // act
                var actual = sut.Period_Active_Days(author);
                // assert
                actual.Should().Be(expected);
            }

            [TestCase("2018-09-11", "2018-09-11", 0)]
            [TestCase("2018-09-10", "2018-09-10", 1)]
            public void WhenBranch_ShouldReturnActiveDays(DateTime start, DateTime end, int expected)
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var branchName = "my-branch";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file2.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-12 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-14 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .On_Branch(branchName)
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithRange(start, end)
                    .WithPath(context.Path)
                    .WithBranch(branchName)
                    .Build();
                // act
                var actual = sut.Period_Active_Days(author);
                // assert
                actual.Should().Be(expected);
            }

            [Test]
            public void WhenEmailNotForActiveDeveloper_ShouldReturnZero()
            {
                // arrange
                var email = "solo@nothere.io";
                var authorName = "no-one";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file2.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-12 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-14 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .Build();
                // act
                var actual = sut.Period_Active_Days(author);
                // assert
                var expected = 0;
                actual.Should().Be(expected);
            }
        }

        [TestFixture]
        public class TotalWorkingDays
        {
            [Test]
            public void WhenDeveloperActiveDuringPeriod_ShouldReturnTotalWorkingDays()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file2.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-12 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-14 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Parse("2018-09-10"), DateTime.Parse("2018-09-14"))
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .Build();
                // act
                var actual = sut.Active_Days_Per_Week(author);
                // assert
                var expectedActiveDaysPerWeek = 3.0;
                actual.Should().Be(expectedActiveDaysPerWeek);
            }

            [Test]
            public void WhenDeveloperNotActiveDuringPeriod_ShouldReturnZero()
            {
                // arrange
                var email = "invalid@buddy.io";
                var authorName = "Moo";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Parse("2018-06-25"), DateTime.Parse("2018-07-09"))
                    .Build();
                // act
                var actual = sut.Active_Days_Per_Week(author);
                // assert
                var expectedActiveDaysPerWeek = 0.0;
                actual.Should().Be(expectedActiveDaysPerWeek);
            }
        }

        [TestFixture]
        public class CommitsPerDay
        {
            [Test]
            public void WhenDeveloperActive_ShouldReturnCommitsPerDay()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file4.txt")
                    .With_File_Content("3", "5")
                    .With_Commit_Timestamp("2018-09-11 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_File_Name("file2.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-12 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit4 = commitBuilder
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-14 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit5 = commitBuilder
                    .With_File_Name("file5.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-14 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Make_Commit(commit4)
                    .Make_Commit(commit5)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Parse("2018-09-10"), DateTime.Parse("2018-09-14"))
                    .Build();
                // act
                var actual = sut.Commits_Per_Day(author);
                // assert
                var expectedCommitsPerDay = 1.25;
                actual.Should().Be(expectedCommitsPerDay);
            }

            [Test]
            public void WhenDeveloperInactive_ShouldReturnZeroCommitsPerDay()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = "boo", Emails = new List<string> { "nono@moon.io" } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Parse("2018-09-10"), DateTime.Parse("2018-09-14"))
                    .Build();
                // act
                var actual = sut.Commits_Per_Day(author);
                // assert
                var expectedCommitsPerDay = 0.0;
                actual.Should().Be(expectedCommitsPerDay);
            }
        }

        [TestFixture]
        public class Build_Individual_Developer_Stats
        {
            [Test]
            public void WhenRangeEntireProjectHistory_ShouldReturnStats()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("3", "4")
                    .With_Commit_Timestamp("2018-09-12 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-20 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Parse("2018-09-10"), DateTime.Parse("2018-09-20"))
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .Build();
                // act
                var actual = sut.Build_Individual_Developer_Stats(new List<Author> { author });
                // assert
                var expected = new List<DeveloperStats>
                {
                    new DeveloperStats
                    {
                        Author = author,
                        ActiveDaysPerWeek = 1.5,
                        PeriodActiveDays = 3,
                        CommitsPerDay = 1.0,
                        Impact = 0.01,
                        LinesOfChangePerHour = 0.33,
                        LinesAdded = 6,
                        LinesRemoved = 2,
                        Rtt100 = 303.03,
                        Ptt100 = 588.24,
                        Churn = 0.33
                    }
                };

                actual.Should().BeEquivalentTo(expected);
            }

            [Test]
            public void WhenNegativePtt100_ShouldReturnAbsOfValue()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var branchName = "negative-commits";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("3", "4", "5", "6")
                    .With_Commit_Timestamp("2018-09-12 11:03:02")
                    .With_Commit_Message("it worked!")
                    .With_Branch(branchName)
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2", "3")
                    .With_Commit_Timestamp("2018-09-13 01:01:01")
                    .With_Commit_Message("it worked!")
                    .With_Branch(branchName)
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .On_Branch(branchName)
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Parse("2018-09-13"), DateTime.Parse("2018-09-13"))
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .WithBranch(branchName)
                    .Build();

                // act
                var actual = sut.Build_Individual_Developer_Stats(new List<Author> { author });
                // assert
                var expected = 833.33;
                var developerStat = actual.FirstOrDefault();
                developerStat.Ptt100.Should().Be(expected);
            }

            [Test]
            public void WhenDeveloperActiveAcrossEntireRange_ShouldReturnStats()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("3", "4")
                    .With_Commit_Timestamp("2018-09-12 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2", "5", "7")
                    .With_Commit_Timestamp("2018-09-20 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Parse("2018-09-10"), DateTime.Parse("2018-09-20"))
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .Build();
                // act
                var actual = sut.Build_Individual_Developer_Stats(new List<Author> { author });
                // assert
                var expected = new List<DeveloperStats>
                {
                    new DeveloperStats
                    {
                        Author = author,
                        ActiveDaysPerWeek = 1.5,
                        PeriodActiveDays = 3,
                        CommitsPerDay = 1.0,
                        Impact = 0.012,
                        LinesOfChangePerHour = 0.42,
                        LinesAdded = 8,
                        LinesRemoved = 2,
                        Churn = 0.25,
                        Rtt100 = 238.1,
                        Ptt100 = 400.0
                    }
                };

                actual.Should().BeEquivalentTo(expected);
            }

            [Test]
            public void WhenDeveloperStatsIncludeFirstCommit_ShouldReturnStatsWithoutException()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp(DateTime.Today)
                    .With_Commit_Message("init commit")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file2.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp(DateTime.Today)
                    .With_Commit_Message("second commit")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Today, DateTime.Today)
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .Build();
                
                // act
                // assert
                Assert.DoesNotThrow(()=>sut.Build_Daily_Individual_Developer_Stats(new List<Author>{author}));
            }

            [Test]
            public void WhenBranchSelected_ShouldReturnAllActiveDevelopersOnBranch()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var branchName = "my-branch";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var commitBuilder = new CommitTestDataBuilder();

                var commit1 = commitBuilder
                    .With_Author("ted", "bob@bunddy.co")
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp(DateTime.Today)
                    .With_Commit_Message("init commit to master")
                    .Build();

                var commit2 = commitBuilder
                    .With_Author(authorName, email)
                    .With_File_Name("file2.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp(DateTime.Today)
                    .With_Commit_Message("first branch commit")
                    .With_Branch(branchName)
                    .Build();

                var commit3 = commitBuilder
                    .With_File_Name("file2.txt")
                    .With_File_Content("3", "5", "abc")
                    .With_Commit_Timestamp(DateTime.Today)
                    .With_Commit_Message("second branch commit")
                    .With_Branch(branchName)
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .Make_Commit(commit1)
                    .On_Branch(branchName)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Today, DateTime.Today)
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .WithBranch(branchName)
                    .Build();
                // act
                var actual = sut.Build_Individual_Developer_Stats(new List<Author> { author });
                // assert
                var expectedNumberOfDevelopersOnBranch = 1;
                actual.Count.Should().Be(expectedNumberOfDevelopersOnBranch);
            }

            // todo : should it count as a period active day if all work is in the ignore list?
            [Test]
            public void WhenPatternsIgnored_ShouldIgnoreMatchingFilesWhenCalculatingDeveloperStats()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt.orig")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("3", "4")
                    .With_Commit_Timestamp("2018-09-10 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("5", "6", "7")
                    .With_Commit_Timestamp("2018-09-10 13:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithIgnorePatterns(new[] {".orig"})
                    .WithRange(DateTime.Parse("2018-09-10"), DateTime.Parse("2018-09-20"))
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .Build();

                // act
                var actual = sut.Build_Individual_Developer_Stats(new List<Author> { author });
                // assert
                var expected = new List<DeveloperStats>
                {
                    new DeveloperStats
                    {
                        Author = author,
                        ActiveDaysPerWeek = 0.5,
                        PeriodActiveDays = 1, 
                        CommitsPerDay = 3.0, // per active day
                        Impact = 0.010, // affected
                        LinesOfChangePerHour = 0.88, // affected
                        LinesAdded = 5, // affected
                        LinesRemoved = 2,
                        Churn = 0.4, // affected
                        Rtt100 = 113.64, // affected
                        Ptt100 = 263.16 // affected
                    }
                };
                actual.Should().BeEquivalentTo(expected);
            }

            [Test]
            public void WhenEntireHistory_ShouldReturnDeveloperStatsForLifetime()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-08-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("3", "4")
                    .With_Commit_Timestamp("2018-09-12 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2", "5", "7")
                    .With_Commit_Timestamp("2018-10-20 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithEntireHistory()
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .Build();
                // act
                var actual = sut.Build_Individual_Developer_Stats(new List<Author> { author });
                // assert
                var expected = new List<DeveloperStats>
                {
                    new DeveloperStats
                    {
                        Author = author,
                        ActiveDaysPerWeek = 0.12,
                        PeriodActiveDays = 3,
                        CommitsPerDay = 1.0,
                        Impact =0.012,
                        LinesOfChangePerHour = 0.42,
                        LinesAdded = 8,
                        LinesRemoved = 2,
                        Churn = 0.25,
                        Rtt100 = 238.1,
                        Ptt100 = 400.00
                    }
                };
                actual.Should().BeEquivalentTo(expected);
            }

            [Test]
            public void WhenUsingAliasMappingForTwoEmails_ShouldReturnOneDeveloperStats()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email, "travis@frisinger.com" } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("3", "4")
                    .With_Commit_Timestamp("2018-09-12 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_Author("Travis", "travis@frisinger.com")
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2", "5", "7")
                    .With_Commit_Timestamp("2018-09-20 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithEntireHistory()
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .Build();
                // act
                var actual = sut.Build_Individual_Developer_Stats(new List<Author> { author });
                // assert
                var expectedNumberOfStats = 1;
                actual.Count.Should().Be(expectedNumberOfStats);
            }
        }

        [TestFixture]
        public class Build_Daily_Individual_Developer_Stats
        {
            [Test]
            public void WhenOneDeveloperForOneDay_ShouldReturnOneDaysStats()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("3", "4")
                    .With_Commit_Timestamp("2018-09-10 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Parse("2018-09-10"), DateTime.Parse("2018-09-10"))
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .Build();
                // act
                var actual = sut.Build_Daily_Individual_Developer_Stats(new List<Author> { author });
                // assert
                var expectedNumberOfDays = 1;
                var expectedNumberOfDevelopers = 1;
                actual.Count.Should().Be(expectedNumberOfDays);
                actual.FirstOrDefault().Stats.Count.Should().Be(expectedNumberOfDevelopers);
            }

            [Test]
            public void WhenManyDaysAndManyAuthors_ShouldReturnEveryDaysStatsForAllAuthors()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email } };
                var author2 = new Author { Name = "bob", Emails = new List<string> { "bob@smith.com" } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("3", "4")
                    .With_Commit_Timestamp("2018-09-11 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_Author("bob","bob@smith.com")
                    .With_File_Name("file2.txt")
                    .With_File_Content("3", "4")
                    .With_Commit_Timestamp("2018-09-12 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Parse("2018-09-10"), DateTime.Parse("2018-09-12"))
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .Build();
                // act
                var actual = sut.Build_Daily_Individual_Developer_Stats(new List<Author> { author, author2 });
                // assert
                var expectedNumberOfDevelopers = 2;
                var expectedNumberOfDays = 3;
                actual.Count.Should().Be(expectedNumberOfDays);
                actual.FirstOrDefault().Stats.Count.Should().Be(expectedNumberOfDevelopers);
            }
        }


        [TestFixture]
        public class Build_Team_Stats
        {
            [Test]
            public void WhenRangeOneDay_ShouldReturnStats()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("3", "4")
                    .With_Commit_Timestamp("2018-09-10 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_Author("bob", "bob@smith.com")
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2", "5", "7")
                    .With_Commit_Timestamp("2018-09-10 12:13:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Parse("2018-09-10"), DateTime.Parse("2018-09-10"))
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .Build();

                // act
                var actual = sut.Build_Team_Stats();
                // assert
                var stats = new List<TeamStats>
                {
                    new TeamStats
                    {
                        DateOf = DateTime.Parse("2018-09-10"),
                        ActiveDevelopers = 2,
                        TotalCommits = 3
                    }
                };
                var expected = new TeamStatsCollection(stats, new List<DayOfWeek>());

                actual.Should().BeEquivalentTo(expected);
            }

            [Test]
            public void WhenRangeOneWeekWithOneAuthor_ShouldReturnStats()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("3", "4")
                    .With_Commit_Timestamp("2018-09-12 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2", "5", "7")
                    .With_Commit_Timestamp("2018-09-14 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Parse("2018-09-10"), DateTime.Parse("2018-09-14"))
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .Build();

                // act
                var actual = sut.Build_Team_Stats();
                // assert
                var stats = new List<TeamStats>
                {
                    new TeamStats
                    {
                        DateOf = DateTime.Parse("2018-09-10"),
                        ActiveDevelopers = 1,
                        TotalCommits = 1
                    },
                    new TeamStats
                    {
                        DateOf = DateTime.Parse("2018-09-11"),
                        ActiveDevelopers = 0,
                        TotalCommits = 0
                    },
                    new TeamStats
                    {
                        DateOf = DateTime.Parse("2018-09-12"),
                        ActiveDevelopers = 1,
                        TotalCommits = 1
                    },
                    new TeamStats
                    {
                        DateOf = DateTime.Parse("2018-09-13"),
                        ActiveDevelopers = 0,
                        TotalCommits = 0
                    },
                    new TeamStats
                    {
                        DateOf = DateTime.Parse("2018-09-14"),
                        ActiveDevelopers = 1,
                        TotalCommits = 1
                    }
                };

                var expected = new TeamStatsCollection(stats, new List<DayOfWeek>());

                actual.Should().BeEquivalentTo(expected);
            }

        }

        // todo : for team stats as well
        [TestFixture]
        public class Ignore_Comments
        {
            [Test]
            public void WhenFalse_ShouldReturnStatsWithCommentedOutLines()
            {
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email, } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("// 1", "// 2")
                    .With_Commit_Timestamp("2018-09-10 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2", "5", "7")
                    .With_Commit_Timestamp("2018-09-10 13:05:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Parse("2018-09-10"), DateTime.Parse("2018-09-10"))
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .WithIgnoreComments(false)
                    .Build();
                // act
                var actual = sut.Build_Individual_Developer_Stats(new List<Author> { author });
                // assert
                var expected = new List<DeveloperStats>
                {
                    new DeveloperStats
                    {
                        Author = author,
                        PeriodActiveDays = 1,
                        Impact = 0.012,
                        Churn = 0.25,
                        LinesAdded = 8,
                        LinesRemoved = 2,
                        ActiveDaysPerWeek = 1,
                        CommitsPerDay = 3,
                        Rtt100 = 80.0,
                        Ptt100 = 133.33,
                        LinesOfChangePerHour = 1.25
                    }
                };

                actual.Should().BeEquivalentTo(expected);
            }

            [Test]
            public void WhenTrue_ShouldReturnStatsIgnoringCommentedOutLines()
            {
                // arrange
                // arrange
                var email = "tmfrisinger@gmail.com";
                var authorName = "T-rav";
                var author = new Author { Name = authorName, Emails = new List<string> { email, } };

                var commitBuilder = new CommitTestDataBuilder()
                    .With_Author(authorName, email);

                var commit1 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("1", "2")
                    .With_Commit_Timestamp("2018-09-10 01:01:01")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit2 = commitBuilder
                    .With_File_Name("file1.txt")
                    .With_File_Content("// 1", "// 2")
                    .With_Commit_Timestamp("2018-09-10 11:03:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var commit3 = commitBuilder
                    .With_File_Name("file3.txt")
                    .With_File_Content("1", "2", "5", "7")
                    .With_Commit_Timestamp("2018-09-10 13:05:02")
                    .With_Commit_Message("it worked!")
                    .Build();

                var context = new RepositoryTestDataBuilder()
                    .After_Init_Commit_To_Master()
                    .Make_Commit(commit1)
                    .Make_Commit(commit2)
                    .Make_Commit(commit3)
                    .Build();

                var sut = new SourceControlAnalysisBuilder()
                    .WithPath(context.Path)
                    .WithRange(DateTime.Parse("2018-09-10"), DateTime.Parse("2018-09-10"))
                    .WithWorkingDaysPerWeek(4)
                    .WithWorkingWeekHours(32)
                    .WithIgnoreComments(true)
                    .Build();
                // act
                var actual = sut.Build_Individual_Developer_Stats(new List<Author> { author });
                // assert
                var expected = new List<DeveloperStats>
                {
                    new DeveloperStats
                    {
                        Author = author,
                        PeriodActiveDays = 1,
                        Impact = 0.006,
                        Churn = 0.33,
                        LinesAdded = 6,
                        LinesRemoved = 2,
                        ActiveDaysPerWeek = 1,
                        CommitsPerDay = 3,
                        Rtt100 = 100.0,
                        Ptt100 = 200.00,
                        LinesOfChangePerHour = 1.0
                    }
                };

                actual.Should().BeEquivalentTo(expected);
            }
        }
        
        private static FileSystemTestArtefact WriteAliasMapping(List<Alias> aliases)
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
            File.WriteAllText(path, aliases.Serialize());

            return new FileSystemTestArtefact { Path = path };
        }
    }
}
