public class RelayStates
{
    public RelayState Relay0 { get; }
    public RelayState Relay1 { get; }

    internal RelayStates(string states)
    {
        Relay0 = (RelayState)int.Parse(states.Substring(0, 1));
        Relay1 = (RelayState)int.Parse(states.Substring(2, 1));
    }
}