using After.Main;

namespace Character
{
    public class PlayerAddedEvent : AbstractEvent
    {
        public Character Player { get; }

        public PlayerAddedEvent(Character player)
        {
            Player = player;
        }
    }
}
