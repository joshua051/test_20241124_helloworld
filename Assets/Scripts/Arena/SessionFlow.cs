namespace IronSand.Arena
{
    // Pure readiness/pause rules. No global clock, cursor or scene side effects.
    public sealed class SessionFlow
    {
        public bool Ready { get; private set; }
        public bool Paused { get; private set; } = true;
        public bool Restarting { get; private set; }
        public string Error { get; private set; }
        public bool CanPlay => Ready && !Paused && !Restarting && Error == null;
        public void MarkReady() { if (Error == null && !Restarting) Ready = true; }
        public void Pause() { Paused = true; }
        public bool Resume(bool ended)
        {
            if (!Ready || ended || Restarting || Error != null) return false;
            Paused = false;
            return true;
        }
        public bool BeginRestart(bool ended)
        {
            if (!Ready || Restarting || Error != null || (!Paused && !ended)) return false;
            Restarting = true;
            Paused = true;
            return true;
        }
        public void Fail(string message)
        {
            Error = string.IsNullOrWhiteSpace(message) ? "Arena setup failed." : message;
            Paused = true;
            Ready = false;
            Restarting = false;
        }
    }
}
