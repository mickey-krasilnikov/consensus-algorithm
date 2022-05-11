using ConsensusAlgorithm.DataAccess.Entities;
using ConsensusAlgorithm.DTO.AppendEntries;

namespace ConsensusAlgorithm.Core.Mappers
{
	public static class LogEntryMapper
	{
		public static LogEntry ToLogEntry(this LogEntity entity)
		{
			return new LogEntry
			{
				Command = entity.Command!,
				Index = entity.Index,
				Term = entity.Term
			};
		}

		public static IList<LogEntry> ToLogEntries(this IEnumerable<LogEntity> entities)
		{
			return entities.Select(ToLogEntry).ToList();
		}

		public static LogEntity ToLogEntity(this LogEntry logEntry)
		{
			return new LogEntity
			{
				Command = logEntry.Command,
				Index = logEntry.Index,
				Term = logEntry.Term
			};
		}

		public static IList<LogEntity> ToLogEntities(this IEnumerable<LogEntry> logEntries)
		{
			return logEntries.Select(ToLogEntity).ToList();
		}
	}
}
