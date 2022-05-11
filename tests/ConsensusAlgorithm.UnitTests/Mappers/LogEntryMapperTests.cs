using ConsensusAlgorithm.Core.Mappers;
using ConsensusAlgorithm.DataAccess.Entities;
using ConsensusAlgorithm.DTO.AppendEntries;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ConsensusAlgorithm.UnitTests.Mappers
{
    [TestFixture]
    public class LogEntryMapperTests
    {
        [TestCaseSource(nameof(ToLogEntryTestCases))]
        public void ToLogEntryTest(LogEntity entity)
        {
            var entry = entity.ToLogEntry();
            entry.Should().NotBeNull();
            entry.Term.Should().Be(entity.Term);
            entry.Command?.Should().Be(entity.Command);
            entry.Index.Should().Be(entity.Index);
        }

        [TestCaseSource(nameof(ToLogEntriesTestCases))]
        public void ToLogEntriesTest(IEnumerable<LogEntity> entities)
        {
            var entries = entities.ToLogEntries();
            entries.Should().NotBeNull();
            foreach (var entity in entities)
            {
                var entry = entries.FirstOrDefault(e => e.Index == entity.Index);
                entry.Should().NotBeNull();
                entry!.Term.Should().Be(entity.Term);
                entry!.Command?.Should().Be(entity.Command);
                entry!.Index.Should().Be(entity.Index);
            }
        }

        [TestCaseSource(nameof(ToLogEntityTestCases))]
        public void ToLogEntityTest(LogEntry entry)
        {
            var entity = entry.ToLogEntity();
            entity.Should().NotBeNull();
            entity.Term.Should().Be(entry.Term);
            entity.Command?.Should().Be(entry.Command);
            entity.Index.Should().Be(entry.Index);
        }

        [TestCaseSource(nameof(ToLogEntitiesTestCases))]
        public void ToLogEntitiesTest(IEnumerable<LogEntry> entries)
        {
            var entities = entries.ToLogEntities();
            entities.Should().NotBeNull();
            foreach (var entry in entries)
            {
                var entity = entities.FirstOrDefault(e => e.Index == entry.Index);
                entity.Should().NotBeNull();
                entity!.Term.Should().Be(entry.Term);
                entity!.Command?.Should().Be(entry.Command);
                entity!.Index.Should().Be(entry.Index);
            }
        }

        public readonly static object[] ToLogEntryTestCases =
        {
            new LogEntity(),
            new LogEntity { Command = "TESTCOMMAND" },
            new LogEntity { Index = 0 },
            new LogEntity { Term = 0 },
            new LogEntity { Command = "TESTCOMMAND", Index = 0, Term = 0 }
        };

        public readonly static object[] ToLogEntriesTestCases =
        {
            new List<LogEntity>(),
            new List<LogEntity> { new LogEntity() },
            new List<LogEntity>
            {
                new LogEntity(),
                new LogEntity { Command = "TESTCOMMAND" },
                new LogEntity { Index = 0 },
                new LogEntity { Term = 0 },
                new LogEntity { Command = "TESTCOMMAND", Index = 0, Term = 0 }
            }
        };

        public readonly static object[] ToLogEntityTestCases =
        {
            new LogEntry(),
            new LogEntry { Command = "TESTCOMMAND" },
            new LogEntry { Index = 0 },
            new LogEntry { Term = 0 },
            new LogEntry { Command = "TESTCOMMAND", Index = 0, Term = 0 }
        };

        public readonly static object[] ToLogEntitiesTestCases =
        {
            new List<LogEntry>(),
            new List<LogEntry> { new LogEntry() },
            new List<LogEntry>
            {
                new LogEntry(),
                new LogEntry { Command = "TESTCOMMAND" },
                new LogEntry { Index = 0 },
                new LogEntry { Term = 0 },
                new LogEntry { Command = "TESTCOMMAND", Index = 0, Term = 0 }
            }
        };
    }
}
