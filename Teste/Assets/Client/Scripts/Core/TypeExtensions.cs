using System;
using System.Collections.Generic;

namespace L5RGame
{
    // Extensions and wrappers for type conversions
    public static class TypeExtensions
    {
        public static CardMoveOptions ToCardMoveOptions(this Dictionary<string, object> dict)
        {
            return new CardMoveOptions(dict);
        }
    }

}
