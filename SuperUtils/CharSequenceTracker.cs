using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperUtils
{
    public class CharSequenceTracker
    {
        private readonly byte[] _sequence;
        private int _matched;
        private bool _complete;

        public int RelativeStart { get; private set; } = -1;

        public CharSequenceTracker(byte[] sequenceToMatch)
        {
            _sequence = sequenceToMatch;
            _matched = 0;
            _complete = false;
        }

        public void NextChar(byte character, int relativeIndex = -1)
        {
            if (_complete) return;

            if (_sequence[_matched] == character)
            {
                if (_matched == 0)
                {
                    RelativeStart = relativeIndex;
                }
                _matched++;
            }
            else
            {
                _matched = 0;
                if (_sequence[0] == character)
                {
                    _matched++;
                }
            }
        }

        public bool Found()
        {
            if (_complete) return false;

            return _matched == _sequence.Length;
        }

        public void Reset()
        {
            _complete = false;
            _matched = 0;
            RelativeStart = -1;
        }

        public void MarkComplete()
        {
            _complete = true;
        }
    }
}
