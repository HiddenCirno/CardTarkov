using System;
using UnityEngine;

namespace CardTarkov.Client.Cards
{
    /// <summary>
    /// 纯代码生成的牌面素材：圆角牌底、牌背、四种花色。
    /// 花色是白色剪影，运行时用 Image.color 染成红/黑。
    /// </summary>
    internal static class CardSprites
    {
        private static Sprite _background;
        private static Sprite _back;
        private static Sprite[] _suits;

        internal static Sprite Background
        {
            get
            {
                if (_background == null)
                    _background = CreateCardBackground(false);
                return _background;
            }
        }

        internal static Sprite Back
        {
            get
            {
                if (_back == null)
                    _back = CreateCardBackground(true);
                return _back;
            }
        }

        internal static Sprite Suit(int suit)
        {
            if (_suits == null || _suits[0] == null)
            {
                _suits = new[]
                {
                    CreateSuit(SuitKind.Spade),
                    CreateSuit(SuitKind.Heart),
                    CreateSuit(SuitKind.Club),
                    CreateSuit(SuitKind.Diamond)
                };
            }

            if (suit < 0 || suit > 3)
                suit = 0;

            return _suits[suit];
        }

        private static Sprite CreateCardBackground(bool back)
        {
            const int width = 96;
            const int height = 132;
            const int radius = 12;
            const int border = 3;

            Color32 fill = back
                ? new Color32(78, 26, 34, 255)
                : new Color32(246, 243, 233, 255);

            Color32 edge = back
                ? new Color32(186, 156, 82, 255)
                : new Color32(110, 106, 96, 255);

            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;

                    if (!InsideRounded(x, y, width, height, radius))
                    {
                        pixels[index] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    bool inner = InsideRounded(
                        x - border, y - border, width - border * 2, height - border * 2,
                        Math.Max(1, radius - border));

                    Color32 color = inner ? fill : edge;

                    if (back && inner && ((x + y) % 14 < 2))
                        color = new Color32(96, 34, 44, 255);

                    pixels[index] = color;
                }
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            return Sprite.Create(
                texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
        }

        private static bool InsideRounded(
            int x, int y, int width, int height, int radius)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return false;

            int cx = x < radius ? radius : x > width - radius - 1 ? width - radius - 1 : x;
            int cy = y < radius ? radius : y > height - radius - 1 ? height - radius - 1 : y;

            int dx = x - cx;
            int dy = y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        private enum SuitKind
        {
            Spade,
            Heart,
            Club,
            Diamond
        }

        private static Sprite CreateSuit(SuitKind kind)
        {
            const int size = 128;
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 像素中心映射到 [-1.15, 1.15]，y 向上
                    double u = ((x + 0.5) / size * 2.0 - 1.0) * 1.15;
                    double v = ((y + 0.5) / size * 2.0 - 1.0) * 1.15;

                    bool inside = InsideSuit(kind, u, v);
                    pixels[y * size + x] = inside ? white : clear;
                }
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            return Sprite.Create(
                texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static bool InsideSuit(SuitKind kind, double u, double v)
        {
            switch (kind)
            {
                case SuitKind.Heart:
                    return Heart(u, v);

                case SuitKind.Spade:
                    return Heart(u, -v) ||
                           (Math.Abs(u) < 0.13 && v < -0.55 && v > -1.12) ||
                           (Math.Abs(u) < 0.42 && v <= -0.98 && v > -1.15);

                case SuitKind.Diamond:
                    return Math.Abs(u) + Math.Abs(v) <= 1.0;

                case SuitKind.Club:
                    return Circle(u, v, 0.0, 0.45, 0.46) ||
                           Circle(u, v, -0.42, -0.22, 0.46) ||
                           Circle(u, v, 0.42, -0.22, 0.46) ||
                           (Math.Abs(u) < 0.13 && v < -0.45 && v > -1.1) ||
                           (Math.Abs(u) < 0.42 && v <= -1.0 && v > -1.15);

                default:
                    return false;
            }
        }

        private static bool Heart(double u, double v)
        {
            double a = u * u + v * v - 1.0;
            return a * a * a - u * u * v * v * v <= 0.0;
        }

        private static bool Circle(double u, double v, double cx, double cy, double r)
        {
            double dx = u - cx;
            double dy = v - cy;
            return dx * dx + dy * dy <= r * r;
        }
    }
}
