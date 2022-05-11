namespace ConsensusAlgorithm.Core.StateMachine
{
	public class DictionaryStateMachine : IStateMachine
	{
		readonly Dictionary<string, int> _state = new();

		public void Apply(string command)
		{
			var commands = command.Split(" ");
			try
			{
				switch (commands[0].ToUpper())
				{
					case "SET": _state[commands[1]] = int.Parse(commands[2]); break;
					case "CLEAR": if (_state.ContainsKey(commands[1])) _state.Remove(commands[1]); break;
				}
			}
			catch (FormatException)
			{
				// Don't apply bad requests
			}
		}

		public string RequestStatus(string param)
		{
			return _state.ContainsKey(param) ? _state[param].ToString() : string.Empty;
		}
	}
}
