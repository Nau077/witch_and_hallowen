using UnityEngine;

public static class ProceduralVFXSprites
{
    // Кэшируем, чтобы не создавать каждый раз
    private static Sprite _fireSmoke16;
    private static Sprite _iceShard16;

    public static Sprite GetFireSmokeSprite16()
    {
        if (_fireSmoke16 != null) return _fireSmoke16;

        // Мягкая "дымка" + горячий центр
        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; // пиксель-арт
        tex.wrapMode = TextureWrapMode.Clamp;

        // Центр
        Vector2 c = new Vector2(7.5f, 7.5f);

        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                // Небольшой jitter, чтобы не было идеального круга
                float jx = (Pseudo01(x, y, 1) - 0.5f) * 0.6f;
                float jy = (Pseudo01(x, y, 2) - 0.5f) * 0.6f;

                float dx = (x - c.x) + jx;
                float dy = (y - c.y) + jy;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / 7.5f; // ~0..1+
                float a = Smooth01(1.15f - r);                  // мягкая альфа

                // Горячее ядро (маленькое)
                float core = Smooth01(0.55f - r);

                // Цвет: серый дым + оранжевое ядро
                Color smoke = new Color(0.75f, 0.75f, 0.75f, 0f);
                Color hot = new Color(1.00f, 0.55f, 0.12f, 0f);

                // Альфа по радиусу
                smoke.a = a * 0.55f;
                hot.a = core * 0.65f;

                // Смешиваем: на краях больше дыма, в центре больше горячего
                float t = core;
                Color col = Color.Lerp(smoke, hot, t);

                // Небольшая "зернистость" в альфе
                float grain = 0.85f + Pseudo01(x, y, 3) * 0.30f;
                col.a *= grain;

                // Отсекаем совсем прозрачное
                if (col.a < 0.03f) col = new Color(0, 0, 0, 0);

                tex.SetPixel(x, y, col);
            }

        tex.Apply(false, true);

        _fireSmoke16 = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        _fireSmoke16.name = "FireSmoke_Procedural_16";
        return _fireSmoke16;
    }

    public static Sprite GetIceShardSprite16()
    {
        if (_iceShard16 != null) return _iceShard16;

        // "Осколок": ромб/кристалл с бликом
        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        // Рисуем ромб (diamond) + highlight
        Vector2 c = new Vector2(7.5f, 7.5f);

        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = Mathf.Abs(x - c.x);
                float dy = Mathf.Abs(y - c.y);

                // ромб: dx + dy <= radius
                float radius = 5.8f;
                float d = (dx + dy) / radius; // <=1 внутри

                if (d > 1.0f)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    continue;
                }

                // Базовый цвет льда
                Color baseCol = new Color(0.70f, 0.95f, 1.00f, 0.0f);

                // Альфа: плотнее внутри
                float a = Smooth01(1.05f - d);
                baseCol.a = a * 0.85f;

                // Блик: диагональная полоска
                float hl = 1f - Mathf.Abs((x - y) * 0.18f);
                hl = Mathf.Clamp01(hl);
                hl *= Smooth01(0.75f - d); // ближе к центру

                Color highlight = new Color(1f, 1f, 1f, hl * 0.65f);

                // Контур чуть ярче
                float edge = Smooth01(d - 0.78f) * 0.35f;
                Color edgeCol = new Color(0.60f, 0.90f, 1.00f, edge);

                Color col = baseCol;
                col = AlphaBlend(col, edgeCol);
                col = AlphaBlend(col, highlight);

                // Слегка "кристальная" зернистость
                col.a *= (0.92f + Pseudo01(x, y, 7) * 0.16f);

                if (col.a < 0.03f) col = new Color(0, 0, 0, 0);
                tex.SetPixel(x, y, col);
            }

        tex.Apply(false, true);

        _iceShard16 = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        _iceShard16.name = "IceShard_Procedural_16";
        return _iceShard16;
    }

    // ---------- helpers ----------

    // Псевдо-рандом 0..1 по координатам (детерминированно)
    private static float Pseudo01(int x, int y, int seed)
    {
        unchecked
        {
            int n = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
            n = (n << 13) ^ n;
            int nn = (n * (n * n * 15731 + 789221) + 1376312589);
            // 0..1
            return Mathf.Abs(nn % 10000) / 10000f;
        }
    }

    private static float Smooth01(float v) => Mathf.Clamp01(v * v * (3f - 2f * v));

    // alpha blend: "over"
    private static Color AlphaBlend(Color under, Color over)
    {
        float a = over.a + under.a * (1f - over.a);
        if (a <= 0f) return new Color(0, 0, 0, 0);

        float r = (over.r * over.a + under.r * under.a * (1f - over.a)) / a;
        float g = (over.g * over.a + under.g * under.a * (1f - over.a)) / a;
        float b = (over.b * over.a + under.b * under.a * (1f - over.a)) / a;

        return new Color(r, g, b, a);
    }
}
