using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DashEnergyBarUI : MonoBehaviour
{
	[Header("Refs")]
	public PlayerDash dash;
	public Image fill;              // Image Type = Filled, Fill Method = Vertical, Origin = Bottom
	public TMP_Text text;           // optional

	[Header("Text")]
	public bool showText = true;
	public bool showAsCurrentMax = true; // true: 35/50, false: 70%
	public bool roundToInt = true;

	[Header("Low energy blink")]
	[Range(0f, 1f)] public float lowEnergyThresholdNormalized = 0.25f;
	public float lowBlinkSpeed = 8f;
	public Color lowEnergyColor = new Color(1f, 0.85f, 0.2f, 1f); // желтоватый
	public Color normalColor = Color.white;

	[Header("No energy flash")]
	public float noEnergyFlashDuration = 0.12f;
	public Color noEnergyFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
	public float noEnergyScalePunch = 0.10f;

	[Header("Low energy pulse (scale)")]
	public float lowScalePulseAmplitude = 0.06f;

	private Vector3 baseScale;
	private Coroutine flashRoutine;

	private void Awake()
	{
		baseScale = transform.localScale;

		if (!dash) dash = FindObjectOfType<PlayerDash>();

		Subscribe();
		RefreshImmediate();
	}

	private void OnEnable()
	{
		Subscribe();
		RefreshImmediate();
	}

	private void OnDisable()
	{
		Unsubscribe();
	}

	private void OnDestroy()
	{
		Unsubscribe();
	}

	private void Subscribe()
	{
		if (dash != null)
			dash.OnDashNoEnergy += HandleDashNoEnergy;
	}

	private void Unsubscribe()
	{
		if (dash != null)
			dash.OnDashNoEnergy -= HandleDashNoEnergy;
	}

	private void Update()
	{
		RefreshImmediate();
		ApplyLowBlink();
	}

	private void RefreshImmediate()
	{
		if (dash == null) return;

		if (fill != null)
			fill.fillAmount = dash.EnergyNormalized;

		if (text != null && showText)
		{
			float cur = dash.CurrentEnergy;
			float max = dash.MaxEnergy;

			if (roundToInt)
			{
				cur = Mathf.Round(cur);
				max = Mathf.Round(max);
			}

			if (showAsCurrentMax)
				text.text = $"{cur}/{max}";
			else
				text.text = $"{Mathf.RoundToInt(dash.EnergyNormalized * 100f)}%";
		}
		else if (text != null)
		{
			text.text = "";
		}
	}

	private void ApplyLowBlink()
	{
		if (dash == null || fill == null) return;

		float n = dash.EnergyNormalized;

		if (n <= lowEnergyThresholdNormalized)
		{
			float wave = Mathf.Sin(Time.time * lowBlinkSpeed) * 0.5f + 0.5f;

			// Мигание цветом (между normalColor и lowEnergyColor)
			fill.color = Color.Lerp(normalColor, lowEnergyColor, wave);

			// Небольшая пульсация скейлом (не мешает флэшу)
			float k = Mathf.Lerp(0f, lowScalePulseAmplitude, wave);
			transform.localScale = baseScale * (1f + k);
		}
		else
		{
			fill.color = normalColor;
			transform.localScale = baseScale;
		}
	}

	private void HandleDashNoEnergy()
	{
		if (!isActiveAndEnabled) return;

		if (flashRoutine != null)
			StopCoroutine(flashRoutine);

		flashRoutine = StartCoroutine(NoEnergyFlashRoutine());
	}

	private IEnumerator NoEnergyFlashRoutine()
	{
		// Короткий “пинок” скейлом + цветом, потом обратно
		Vector3 startScale = transform.localScale;

		if (fill != null)
			fill.color = noEnergyFlashColor;

		transform.localScale = baseScale * (1f + noEnergyScalePunch);

		float t = 0f;
		float dur = Mathf.Max(0.01f, noEnergyFlashDuration);

		while (t < dur)
		{
			t += Time.deltaTime;
			yield return null;
		}

		// Возвращаем — но не ломаем low-blink: он сам выставит цвет/scale на следующем Update
		transform.localScale = startScale;
		flashRoutine = null;
	}
}
