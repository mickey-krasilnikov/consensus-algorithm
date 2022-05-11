namespace ConsensusAlgorithm.Core.StateMachine
{
	public interface IStateMachine
	{
		void Apply(string command);

		string RequestStatus(string param);
	}
}
