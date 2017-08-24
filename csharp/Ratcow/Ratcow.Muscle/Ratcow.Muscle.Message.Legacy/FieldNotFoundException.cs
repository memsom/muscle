namespace Ratcow.Muscle.Message.Legacy
{
    public class FieldNotFoundException : MessageException
    {
        public FieldNotFoundException(string s) : base(s) { }
        public FieldNotFoundException() : base("Message entry not found") { }
    }
}
