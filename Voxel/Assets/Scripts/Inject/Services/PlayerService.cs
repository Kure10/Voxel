using System.Collections.Generic;
using Character;
using PlayerCharacter = Character.Character;

namespace After.Main
{
    public class PlayerService : IService
    {
        [Inject] private MyEventManager _eventManager;

        private readonly List<PlayerCharacter> _players = new();

        public IReadOnlyList<PlayerCharacter> Players => _players;

        public void Init()
        {
            _eventManager.AddListener<PlayerAddedEvent>(OnPlayerAdded);
            _eventManager.AddListener<PlayerRemovedEvent>(OnPlayerRemoved);
        }

        private void OnPlayerAdded(PlayerAddedEvent playerEvent)
        {
            if (playerEvent.Player != null && !_players.Contains(playerEvent.Player))
                _players.Add(playerEvent.Player);
        }

        private void OnPlayerRemoved(PlayerRemovedEvent playerEvent)
        {
            if (playerEvent.Player != null)
                _players.Remove(playerEvent.Player);
        }

        public void Destroy()
        {
            _eventManager.RemoveListener<PlayerAddedEvent>(OnPlayerAdded);
            _eventManager.RemoveListener<PlayerRemovedEvent>(OnPlayerRemoved);
            _players.Clear();
        }
    }
}
