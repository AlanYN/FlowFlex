using System;
using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Utils
{

    /// <summary>
    /// Parse enum values
    /// </summary>
    public class BitMaskManager
    {
        public static List<T> ParseBitFlags<T>(long value) where T : Enum
        {
            Type enumType = typeof(T);
            List<T> enumValues = [];

            foreach (T possibleValue in Enum.GetValues(enumType))
            {
                int enumValue = Convert.ToInt32(possibleValue);
                if ((value & enumValue) != 0)
                {
                    enumValues.Add(possibleValue);
                }
            }

            return enumValues;
        }
    }
}
