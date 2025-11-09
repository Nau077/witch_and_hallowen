using UnityEngine;

/// <summary>
/// Общий интерфейс для всех снарядов игрока/врагов.
/// Обязателен, чтобы стрелок мог инициализировать полёт без знания конкретного класса.
/// </summary>
public interface IProjectile
{
    /// <param name="dir">Нормализованное направление полёта.</param>
    /// <param name="distance">Сколько метров пролететь (используется для расчёта lifetime).</param>
    /// <param name="speedOverride">Если >0 — переопределяет скорость на полёте.</param>
    /// <param name="ignoreFirstMeters">Сколько первых метров игнорировать попадания по врагам.</param>
    void Init(Vector2 dir, float distance, float speedOverride = -1f, float ignoreFirstMeters = 0f);
}
