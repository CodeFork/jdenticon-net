﻿#region License
//
// Jdenticon-net
// https://github.com/dmester/jdenticon-net
// Copyright © Daniel Mester Pirttijärvi 2016
//
// This software is provided 'as-is', without any express or implied
// warranty.In no event will the authors be held liable for any damages
// arising from the use of this software.
// 
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software.If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
// 
// 2. Altered source versions must be plainly marked as such, and must not be
//    misrepresented as being the original software.
// 
// 3. This notice may not be removed or altered from any source distribution.
//
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace Jdenticon.Rendering
{
    /// <summary>
    /// Represents a 24-bit color with a 8-bit alpha channel.
    /// </summary>
    public partial struct Color : IFormattable, IEquatable<Color>
    {
        #region Fields

        // Stored as RGBA
        private uint value;

        #endregion

        #region Constructors

        // Users of the struct should use the static factory methods to create Color value.

        private Color(int a, int r, int g, int b)
        {
            value =
                (((uint)r) << 24) |
                (((uint)g) << 16) |
                (((uint)b) << 8) |
                (((uint)a));
        }

        private Color(uint rgba)
        {
            value = rgba;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the red component of this color in the range [0,255].
        /// </summary>
        public int R
        {
            get { return (int)(value >> 24); }
        }

        /// <summary>
        /// Gets the green component of this color in the range [0,255].
        /// </summary>
        public int G
        {
            get { return (int)((value >> 16) & 0xff); }
        }

        /// <summary>
        /// Gets the blue component of this color in the range [0,255].
        /// </summary>
        public int B
        {
            get { return (int)((value >> 8) & 0xff); }
        }

        /// <summary>
        /// Gets the alpha channel value of this color in the range [0,255] where 0 is fully transparent.
        /// </summary>
        public int A
        {
            get { return (int)(value & 0xff); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a <see cref="Color"/> from an ARGB value.
        /// </summary>
        /// <param name="a">Alpha channel value in the range [0,255].</param>
        /// <param name="r">Red component in the range [0,255].</param>
        /// <param name="g">GReen component in the range [0,255].</param>
        /// <param name="b">Blue component in the range [0,255].</param>
        public static Color FromArgb(int a, int r, int g, int b)
        {
            return new Color(a, r, g, b);
        }

        /// <summary>
        /// Creates a <see cref="Color"/> from an ARGB value.
        /// </summary>
        public static Color FromArgb(uint argb)
        {
            return new Color(argb);
        }

        /// <summary>
        /// Creates a <see cref="Color"/> instance from HSL color parameters.
        /// </summary>
        /// <param name="hue">Hue in the range [0, 1]</param>
        /// <param name="saturation">Saturation in the range [0, 1]</param>
        /// <param name="lightness">Lightness in the range [0, 1]</param>
        public static Color FromHsl(float hue, float saturation, float lightness)
        {
            if (hue < 0) throw new ArgumentOutOfRangeException("hue");
            else if (hue > 1) throw new ArgumentOutOfRangeException("hue");

            if (saturation < 0) throw new ArgumentOutOfRangeException("saturation");
            else if (saturation > 1) throw new ArgumentOutOfRangeException("saturation");

            if (lightness < 0) throw new ArgumentOutOfRangeException("lightness");
            else if (lightness > 1) throw new ArgumentOutOfRangeException("lightness");

            // Based on http://www.w3.org/TR/2011/REC-css3-color-20110607/#hsl-color
            if (saturation == 0)
            {
                var value = (int)(lightness * 255);
                return Color.FromArgb(255, value, value, value);
            }
            else
            {
                var m2 = lightness <= 0.5f ? lightness * (saturation + 1) : lightness + saturation - lightness * saturation;
                var m1 = lightness * 2 - m2;

                return FromArgb(255,
                    HueToRgb(m1, m2, hue * 6 + 2),
                    HueToRgb(m1, m2, hue * 6),
                    HueToRgb(m1, m2, hue * 6 - 2));
            }
        }

        // Helper method for FromHsl
        private static int HueToRgb(float m1, float m2, float h)
        {
            h = h < 0 ? h + 6 : h > 6 ? h - 6 : h;
            return (int)(255 * (
                h < 1 ? m1 + (m2 - m1) * h :
                h < 3 ? m2 :
                h < 4 ? m1 + (m2 - m1) * (4 - h) :
                m1));
        }

        /// <summary>
        /// Blends this color with another color using the over blending operation.
        /// </summary>
        /// <param name="background">The background color.</param>
        public Color Over(Color background)
        {
            var foreA = A;

            if (foreA < 1)
            {
                return background;
            }
            else if (foreA > 254 || background.A < 1)
            {
                return this;
            }

            // Source: https://en.wikipedia.org/wiki/Alpha_compositing#Description
            var forePA = foreA * 255;
            var backPA = background.A * (255 - foreA);
            var alpha = (forePA + backPA);

            var b = (byte)(
                (forePA * B + backPA * background.B) /
                alpha);

            var g = (byte)(
                (forePA * G + backPA * background.G) /
                alpha);

            var r = (byte)(
                (forePA * R + backPA * background.R) /
                alpha);

            var a = (byte)(alpha / 255);

            return new Color(a, r, g, b);
        }

        /// <summary>
        /// Gets the argb value of this color.
        /// </summary>
        public uint ToArgb()
        {
            return value;
        }

        /// <summary>
        /// Gets a hexadecimal representation of this color on the format #rrggbbaa.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "#" + value.ToString("x8");
        }

        /// <summary>
        /// Gets a string representation of this color on the specified format. The following decimal
        /// placeholders are recognized: R, G, B, A. The following hexadecimal placeholders are
        /// recognized: RR, GG, BB, AA, rr, gg, bb, aa, where the lower case keywords produces 
        /// lower case hex strings.
        /// </summary>
        public string ToString(string format)
        {
            return ToString(format, null);
        }

        /// <summary>
        /// Gets a string representation of this color on the specified format. The following decimal
        /// placeholders are recognized: R, G, B, A. The following hexadecimal placeholders are
        /// recognized: RR, GG, BB, AA, rr, gg, bb, aa, where the lower case keywords produces 
        /// lower case hex strings.
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));

            var sb = new StringBuilder(format.Length * 3);
            var keywords = new char[] { 'R', 'G', 'B', 'A', 'r', 'g', 'b', 'a' };

            var formatCursor = 0;

            while (formatCursor < format.Length)
            {
                var nextIndex = format.IndexOfAny(keywords, formatCursor);
                if (nextIndex >= 0)
                {
                    // Add substring preceding placeholder
                    sb.Append(format, formatCursor, nextIndex - formatCursor);
                    formatCursor = nextIndex + 1;

                    // Process placeholder
                    int value;
                    bool isUpperCase;

                    switch (format[nextIndex])
                    {
                        case 'R': value = R; isUpperCase = true; break;
                        case 'G': value = G; isUpperCase = true; break;
                        case 'B': value = B; isUpperCase = true; break;
                        case 'A': value = A; isUpperCase = true; break;
                        case 'r': value = R; isUpperCase = false; break;
                        case 'g': value = G; isUpperCase = false; break;
                        case 'b': value = B; isUpperCase = false; break;
                        case 'a': value = A; isUpperCase = false; break;
                        default: throw new Exception("Unknown placeholder."); // << this should not happen
                    }

                    var isHexPlaceholder =
                        nextIndex + 1 < format.Length &&
                        format[nextIndex] == format[nextIndex + 1];

                    if (isHexPlaceholder)
                    {
                        formatCursor++; 
                        sb.Append(value.ToString(isUpperCase ? "X2" : "x2", formatProvider));
                    }
                    else if (isUpperCase)
                    {
                        sb.Append(value.ToString(formatProvider));
                    }
                    else
                    {
                        // Not a placeholder
                        sb.Append(format[nextIndex]);
                    }
                }
                else
                {
                    break;
                }
            }
            
            // End of string
            if (formatCursor < format.Length)
            {
                sb.Append(format, formatCursor, format.Length - formatCursor);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the RGBA value which serves as hash for this color.
        /// </summary>
        public override int GetHashCode()
        {
            return unchecked((int)value);
        }

        /// <summary>
        /// Checks if this color has the same RGBA value as another color.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Color && Equals((Color)obj);
        }

        /// <summary>
        /// Checks if this color has the same RGBA value as another color.
        /// </summary>
        public bool Equals(Color other)
        {
            return other.value == value;
        }

        /// <summary>
        /// Checks if the two <see cref="Color"/> have the same RGBA value.
        /// </summary>
        public static bool operator ==(Color a, Color b)
        {
            return a.value == b.value;
        }

        /// <summary>
        /// Checks if the two <see cref="Color"/> have different RGBA value.
        /// </summary>
        public static bool operator !=(Color a, Color b)
        {
            return a.value != b.value;
        }

        #endregion
    }
}