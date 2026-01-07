using UnityEngine;

[DisallowMultipleComponent]
public class IceCrystalTrail : ProjectileParticleTrailBase
{
	[Header("Ice crystal look")]
	public float startSizeMin = 0.06f;
	public float startSizeMax = 0.12f;

	// ❄️ более голубой
	public Color startColor = new Color(0.35f, 0.85f, 1.00f, 0.45f);
	public Color endColor = new Color(0.35f, 0.85f, 1.00f, 0f);

	protected override void Reset()
	{
		sortingLayerName = "FX";
		orderInLayer = 11;

		// вниз (чуть мягче, чем у огня)
		localOffset = new Vector2(0f, -0.10f);

		worldSpace = true;
		emissionRate = 60f;
		lifetime = 0.28f;
		startSpeed = 0.10f;
		spawnRadius = 0.03f;

		if (particleSprite == null)
			particleSprite = ProceduralVFXSprites.GetIceShardSprite16();

		base.Reset();
		ApplyLineTrailPreset();
	}

	protected override void OnValidate()
	{
		if (particleSprite == null)
			particleSprite = ProceduralVFXSprites.GetIceShardSprite16();

		base.OnValidate();
		ApplyLineTrailPreset();
	}

	protected override void ApplyTypeSpecific()
	{
		var main = ps.main;
		main.startSize = new ParticleSystem.MinMaxCurve(startSizeMin, startSizeMax);
		main.startSpeed = Mathf.Max(0.06f, startSpeed * 1.1f);

		var col = ps.colorOverLifetime;
		col.enabled = true;

		var grad = new Gradient();
		grad.SetKeys(
			new[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
			new[] { new GradientAlphaKey(startColor.a, 0f), new GradientAlphaKey(0f, 1f) }
		);
		col.color = new ParticleSystem.MinMaxGradient(grad);

		var rot = ps.rotationOverLifetime;
		rot.enabled = true;
		rot.separateAxes = false;
		rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

		var sol = ps.sizeOverLifetime;
		sol.enabled = true;
		sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
			new Keyframe(0f, 1f),
			new Keyframe(1f, 0f)
		));
	}

	private void ApplyLineTrailPreset()
	{
		var line = GetComponent<DefaultLineTrail>();
		if (line == null) return;

		line.sortingLayerName = "FX";
		line.orderInLayer = 10;

		// ❄️ холодный голубой хвост
		line.startColor = new Color(0.35f, 0.85f, 1.00f, 0.70f);
		line.endColor = new Color(0.35f, 0.85f, 1.00f, 0f);

		line.startWidth = 0.18f;
		line.endWidth = 0.00f;
		line.time = 0.18f;

		line.localOffset = new Vector2(0f, -0.10f);

		line.ApplyOrCreate();
	}
}
