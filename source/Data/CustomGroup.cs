using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DuplicateHider.Data
{
    [JsonObject]
    public class CustomGroup : IIdentifiable, IEnumerable<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public List<Guid> Games { get; } = new List<Guid>();
        public bool ScoreByOrder { get; set; } = false;

        public CustomGroup()
        {

        }

        public CustomGroup(IEnumerable<Game> games)
        {
            foreach(var game in games)
            {
                AddGame(game);
            }
        }

        public CustomGroup(IEnumerable<Guid> games)
        {
            foreach (var game in games)
            {
                AddGame(game);
            }
        }

        public bool Contains(Game game)
        {
            return Contains(game.Id);
        }

        public bool Contains(Guid game)
        {
            return Games.Contains(game);
        }

        public bool AddGame(Game game)
        {
            return AddGame(game.Id);
        }

        public bool RemoveGame(Game game)
        {
            return RemoveGame(game.Id);
        }

        public bool AddGame(Guid id)
        {
            if (Games.Contains(id))
            {
                return false;
            }
            Games.Add(id);
            return true;
        }

        public bool RemoveGame(Guid id)
        {
            return Games.Remove(id);
        }

        public IEnumerator<Guid> GetEnumerator()
        {
            return Games.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Games).GetEnumerator();
        }

        public bool Transfer(CustomGroup target, Guid id)
        {
            return Transfer(this, target, id);
        }

        static public bool Transfer(CustomGroup source, CustomGroup target, Guid id)
        {
            if (source.Contains(id) && !target.Contains(id))
            {
                return source.RemoveGame(id) && target.AddGame(id);
            }
            return false;
        }
    }
}
