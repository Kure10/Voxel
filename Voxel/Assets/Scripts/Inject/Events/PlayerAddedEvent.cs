using After.Main;
using Character;

namespace After.Main
{
    public class PlayerAddedEvent : AbstractEvent
    {
        public Character.Character Player { get; }

        public PlayerAddedEvent(Character.Character player)
        {
            Player = player;
        }
    }
}