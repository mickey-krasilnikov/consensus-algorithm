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
        [TestCase(1, 1, "")]
        [TestCase(2, 2, "TESTCOMMAND")]
        public void ToLogEntryTest(int term, int index, string command)
        {
            //Arrange
            var entity = new LogEntity() { Term = term, Index = index, Command = command };

            //Act
            var entry = entity.ToLogEntry();

            //Assert
            entry.Should().NotBeNull();
            entry.Term.Should().Be(entity.Term);
            entry.Command?.Should().Be(entity.Command);
            entry.Index.Should().Be(entity.Index);
        }

        [TestCaseSource(nameof(ToLogEntriesTestCases))]
        public void ToLogEntriesTest(List<LogEntity> entities)
        {
            //Act
            var entries = entities.ToLogEntries();
            entries.Should().NotBeNull();

            //Assert
            foreach (var entity in entities)
            {
                var entry = entries.FirstOrDefault(e => e.Index == entity.Index);

                entry.Should().NotBeNull();
                entry!.Term.Should().Be(entity.Term);
                entry!.Command?.Should().Be(entity.Command);
                entry!.Index.Should().Be(entity.Index);
            }
        }

        [TestCase(1, 1, "")]
        [TestCase(2, 2, "TESTCOMMAND")]
        public void ToLogEntityTest(int term, int index, string command)
        {
            //Arrange
            var entry = new LogEntry() { Term = term, Index = index, Command = command };

            //Act
            var entity = entry.ToLogEntity();

            //Assert
            entity.Should().NotBeNull();
            entity.Term.Should().Be(entry.Term);
            entity.Command?.Should().Be(entry.Command);
            entity.Index.Should().Be(entry.Index);
        }

        [TestCaseSource(nameof(ToLogEntitiesTestCases))]
        public void ToLogEntitiesTest(List<LogEntry> entries)
        {
            //Act
            var entities = entries.ToLogEntities();

            //Assert
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

        public readonly static object[] ToLogEntriesTestCases =
        {
            new TestCaseData(new List<LogEntity>()) { TestName = "ToLogEntriesTest(Empty List Of LogEntity)" },
            new TestCaseData(new List<LogEntity> { new LogEntity() }) { TestName = "ToLogEntriesTest(List With One Empty LogEntity)" },
            new TestCaseData
            (
                new List<LogEntity>
                {
                    new LogEntity(),
                    new LogEntity { Command = string.Empty, Index=0, Term = 0  },
                    new LogEntity { Command = "TESTCOMMAND", Index = 0, Term = 0 }
                }
            ) { TestName = "ToLogEntriesTest(List Of LogEntity)"}
        };

        public readonly static object[] ToLogEntitiesTestCases =
        {
            new TestCaseData(new List<LogEntry>()) { TestName = "ToLogEntitiesTest(Empty List Of LogEntity)" },
            new TestCaseData(new List<LogEntry> { new LogEntry() }) { TestName = "ToLogEntitiesTest(List With One Empty LogEntity)" },
            new TestCaseData
            (
                new List<LogEntry>
                {
                    new LogEntry(),
                    new LogEntry { Command = string.Empty, Index=0, Term = 0  },
                    new LogEntry { Command = "TESTCOMMAND", Index = 0, Term = 0 }
                }
            ) { TestName = "ToLogEntitiesTest(List Of LogEntity)" }
        };
    }
}
