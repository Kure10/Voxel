using After.Main;

namespace Character
{
    public class PlayerRemovedEvent : AbstractEvent
    {
        public Character Player { get; }

        public PlayerRemovedEvent(Character player)
        {
            Player = player;
        }
    }
}
