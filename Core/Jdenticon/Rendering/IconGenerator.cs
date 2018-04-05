﻿#region License
// Jdenticon-net
// https://github.com/dmester/jdenticon-net
// Copyright © Daniel Mester Pirttijärvi 2016
//
// Permission is hereby granted, free of charge, to any person obtaining 
// a copy of this software and associated documentation files (the 
// "Software"), to deal in the Software without restriction, including 
// without limitation the rights to use, copy, modify, merge, publish, 
// distribute, sublicense, and/or sell copies of the Software, and to 
// permit persons to whom the Software is furnished to do so, subject to 
// the following conditions:
// 
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY 
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion

using Jdenticon.Shapes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jdenticon.Rendering
{
    /// <summary>
    /// Generates identicons and render them to a <see cref="Renderer"/>. This class
    /// dictates what shapes will be used in the generated icons. If you intend to 
    /// customize the appearance of generated icons you probably wants to either subclass
    /// or modify this class.
    /// </summary>
    public class IconGenerator
    {
        private static readonly ShapeCategory[] DefaultCategories = new[]
        {
            // Sides
            new ShapeCategory
            {
                ColorIndex = 8,
                Shapes = ShapeDefinitions.OuterShapes,
                ShapeIndex = 2,
                RotationIndex = 3,
                Positions = new ShapePositionCollection {{1, 0}, {2, 0}, {2, 3}, {1, 3}, {0, 1}, {3, 1}, {3, 2}, {0, 2}}
            },

            // Corners
            new ShapeCategory
            {
                ColorIndex = 9,
                Shapes = ShapeDefinitions.OuterShapes,
                ShapeIndex = 4,
                RotationIndex = 5,
                Positions = new ShapePositionCollection {{0, 0}, {3, 0}, {3, 3}, {0, 3}}
            },

            // Center
            new ShapeCategory
            {
                ColorIndex = 10,
                Shapes = ShapeDefinitions.CenterShapes,
                ShapeIndex = 1,
                RotationIndex = null,
                Positions = new ShapePositionCollection {{1, 1}, {2, 1}, {2, 2}, {1, 2}}
            }
        };
        
        /// <summary>
        /// Gets the number of cells in each direction of the icons generated by this <see cref="IconGenerator"/>.
        /// </summary>
        public virtual int CellCount
        {
            get { return 4; }
        }

        /// <summary>
        /// Determines the hue to be used in an icon for the specified hash.
        /// </summary>
        /// <param name="hash">Hash for which the hue will be computed. Must contain at least 4 bytes.</param>
        protected static float GetHue(byte[] hash)
        {
            var bytes = new byte[4];

            Array.Copy(hash, hash.Length - 4, bytes, 0, 4);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            var value = BitConverter.ToUInt32(bytes, 0) & 0xfffffff;

            return (float)value / 0xfffffff;
        }

        /// <summary>
        /// Determines whether <paramref name="newValue"/> is duplicated in <paramref name="source"/>
        /// if all values in <paramref name="duplicateValues"/> are determined to be equal.
        /// </summary>
        private static bool IsDuplicate(ICollection<int> source, int newValue, params int[] duplicateValues)
        {
            if (((ICollection<int>)duplicateValues).Contains(newValue))
            {
                for (var i = 0; i < duplicateValues.Length; i++)
                {
                    if (source.Contains(duplicateValues[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the specified octet from a byte array.
        /// </summary>
        /// <param name="source">The array from which the octet will be retrieved.</param>
        /// <param name="index">The zero-based index of the octet to be returned.</param>
        protected static byte GetOctet(byte[] source, int index)
        {
            var byteIndex = index / 2;
            var byteValue = source[byteIndex];

            if (byteIndex * 2 == index)
            {
                byteValue = (byte)(byteValue >> 4);
            }
            else
            {
                byteValue = (byte)(byteValue & 0xf);
            }

            return byteValue;
        }

        /// <summary>
        /// Gets an enumeration of the shape categories to be rendered in icons generated by
        /// this <see cref="IconGenerator"/>.
        /// </summary>
        protected virtual IEnumerable<ShapeCategory> GetCategories()
        {
            return DefaultCategories;
        }

        /// <summary>
        /// Gets an enumeration of individual shapes to be rendered in an icon for a specific hash.
        /// </summary>
        /// <param name="colorTheme">A color theme specifying the colors to be used in the icon.</param>
        /// <param name="hash">The hash for which the shapes will be returned.</param>
        protected virtual IEnumerable<Shape> GetShapes(ColorTheme colorTheme, byte[] hash)
        {
            var usedColorThemeIndexes = new List<int>();
            
            foreach (var category in GetCategories())
            {
                var colorThemeIndex = GetOctet(hash, category.ColorIndex) % colorTheme.Count;

                if (IsDuplicate(usedColorThemeIndexes, colorThemeIndex, 0, 4) || // Disallow dark gray and dark color combo
                    IsDuplicate(usedColorThemeIndexes, colorThemeIndex, 2, 3))   // Disallow light gray and light color combo
                {
                    colorThemeIndex = 1;
                }

                usedColorThemeIndexes.Add(colorThemeIndex);

                var startRotationIndex = category.RotationIndex == null ? 0 : GetOctet(hash, category.RotationIndex.Value);
                var shape = category.Shapes[GetOctet(hash, category.ShapeIndex) % category.Shapes.Count];
                
                yield return new Shape
                {
                    Definition = shape,
                    Color = colorTheme[colorThemeIndex],
                    Positions = category.Positions,
                    StartRotationIndex = startRotationIndex
                };
            }
        }

        /// <summary>
        /// Creates a quadratic copy of the specified <see cref="Rectangle"/> with a 
        /// multiple of the cell count as size.
        /// </summary>
        /// <param name="rect">The rectangle to be normalized.</param>
        protected Rectangle NormalizeRectangle(Rectangle rect)
        {
            var size = Math.Min(rect.Width, rect.Height);
            
            // Make size a multiple of the cell count
            size -= size % CellCount;
            
            return new Rectangle(
                x: rect.X + (rect.Width - size) / 2,
                y: rect.Y + (rect.Height - size) / 2,
                width: size,
                height: size
                );
        }

        /// <summary>
        /// Renders the background of an icon.
        /// </summary>
        /// <param name="renderer">The <see cref="Renderer"/> to be used for rendering the icon on the target surface.</param>
        /// <param name="rect">The outer bounds of the icon.</param>
        /// <param name="style">The style of the icon.</param>
        /// <param name="colorTheme">A color theme specifying the colors to be used in the icon.</param>
        /// <param name="hash">The hash to be used as basis for the generated icon.</param>
        protected virtual void RenderBackground(Renderer renderer, Rectangle rect, IdenticonStyle style, ColorTheme colorTheme, byte[] hash)
        {
            renderer.SetBackground(style.BackColor);
        }

        /// <summary>
        /// Renders the foreground of an icon.
        /// </summary>
        /// <param name="renderer">The <see cref="Renderer"/> to be used for rendering the icon on the target surface.</param>
        /// <param name="rect">The outer bounds of the icon.</param>
        /// <param name="style">The style of the icon.</param>
        /// <param name="colorTheme">A color theme specifying the colors to be used in the icon.</param>
        /// <param name="hash">The hash to be used as basis for the generated icon.</param>
        protected virtual void RenderForeground(Renderer renderer, Rectangle rect, IdenticonStyle style, ColorTheme colorTheme, byte[] hash)
        {
            // Ensure rect is quadratic and a multiple of the cell count
            var normalizedRect = NormalizeRectangle(rect);
            var cellSize = normalizedRect.Width / CellCount;

            foreach (var shape in GetShapes(colorTheme, hash))
            {
                var rotation = shape.StartRotationIndex;

                using (renderer.BeginShape(shape.Color))
                {
                    for (var i = 0; i < shape.Positions.Count; i++)
                    {
                        var position = shape.Positions[i];

                        renderer.Transform = new Transform(
                            normalizedRect.X + position.X * cellSize,
                            normalizedRect.Y + position.Y * cellSize,
                            cellSize, rotation++ % 4);

                        shape.Definition(renderer, cellSize, i);
                    }
                }
            }
        }

        /// <summary>
        /// Generates an identicon for the specified hash.
        /// </summary>
        /// <param name="renderer">The <see cref="Renderer"/> to be used for rendering the icon on the target surface.</param>
        /// <param name="rect">The outer bounds of the icon.</param>
        /// <param name="style">The style of the icon.</param>
        /// <param name="hash">The hash to be used as basis for the generated icon.</param>
        public void Generate(Renderer renderer, Rectangle rect, IdenticonStyle style, byte[] hash)
        {
            var hue = GetHue(hash);
            var colorTheme = new ColorTheme(hue, style);

            RenderBackground(renderer, rect, style, colorTheme, hash);
            RenderForeground(renderer, rect, style, colorTheme, hash);
        }
    }
}
