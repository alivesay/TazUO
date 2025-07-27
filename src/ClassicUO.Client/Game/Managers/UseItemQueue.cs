// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;
using ClassicUO.Configuration;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    public sealed class UseItemQueue
    {
        private readonly Deque<uint> _actions = new Deque<uint>();
        private long _timer;
        private readonly World _world;
        private static long _delay = 1000;

        public UseItemQueue(World world)
        {
            _delay = ProfileManager.CurrentProfile.MoveMultiObjectDelay;
            _timer = Time.Ticks + _delay;
            _world = world;
        }

        public void Update()
        {
            if (_timer < Time.Ticks)
            {
                _timer = Time.Ticks + _delay;

                if (_actions.Count == 0)
                {
                    return;
                }

                uint serial = _actions.RemoveFromFront();
                GameActions.DoubleClick(_world, serial);
            }
        }

        public void Add(uint serial)
        {
            foreach (uint s in _actions)
            {
                if (serial == s)
                {
                    return;
                }
            }

            _actions.AddToBack(serial);
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public void ClearCorpses()
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                Entity entity = _world.Get(_actions[i]);

                if (entity == null)
                {
                    continue;
                }

                if (entity is Item it && it.IsCorpse)
                {
                    _actions.RemoveAt(i--);
                }
            }
        }
    }
}