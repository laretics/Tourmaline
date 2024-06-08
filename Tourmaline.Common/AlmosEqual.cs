using System;
namespace TOURMALINE.Common
{
    public static class AlmostEqualE
    {
        /// <summary>
        /// Returns true when the floating point value is *close to* the given value,
        /// within a given tolerance.
        /// </summary>
        /// <param name="thisValue"></param>
        /// <param name="value">The value to compare with.</param>
        /// <param name="tolerance">The amount the two values may differ while still being considered equal</param>
        /// <returns></returns>
        public static bool AlmostEqual(this float thisValue, float value, float tolerance)
        {
            bool returnValue = false;

            if (Math.Abs(thisValue - value) <= tolerance)
            {
                returnValue = true;
            }

            return returnValue;
        }
    }
}
