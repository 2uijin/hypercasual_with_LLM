using UnityEngine;

namespace PrisonLife.FX
{
    /// <summary>
    /// Runtime factory for procedurally-configured ParticleSystems. Each script that needs an
    /// effect builds one on Start via this helper — no scene/prefab wiring required.
    /// </summary>
    public static class FxSpawner
    {
        private static Material _matAdditive;
        private static Material _matAlpha;
        private static Mesh _cubeMesh;

        private static Material MatAdditive
        {
            get
            {
                if (_matAdditive == null)
                {
                    var sh = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Particles/Additive") ?? Shader.Find("Sprites/Default");
                    _matAdditive = new Material(sh) { name = "FxAdditive" };
                }
                return _matAdditive;
            }
        }

        private static Material MatAlpha
        {
            get
            {
                if (_matAlpha == null)
                {
                    var sh = Shader.Find("Legacy Shaders/Particles/Alpha Blended") ?? Shader.Find("Particles/Alpha Blended") ?? Shader.Find("Sprites/Default");
                    _matAlpha = new Material(sh) { name = "FxAlpha" };
                }
                return _matAlpha;
            }
        }

        private static Mesh CubeMesh
        {
            get
            {
                if (_cubeMesh == null)
                {
                    var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _cubeMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
                    if (Application.isPlaying) Object.Destroy(tmp); else Object.DestroyImmediate(tmp);
                }
                return _cubeMesh;
            }
        }

        private static ParticleSystem NewPs(Transform parent, Vector3 localPos, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.AddComponent<ParticleSystem>();
        }

        public static ParticleSystem CreateSpark(Transform parent, Vector3 localPos)
        {
            var ps = NewPs(parent, localPos, "Fx_Spark");
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.material = MatAdditive;
            var m = ps.main;
            m.startLifetime = 0.3f;
            m.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            m.startSize = 0.06f;
            m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.3f), Color.white);
            m.playOnAwake = false;
            m.loop = false;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.1f;
            ps.Stop();
            return ps;
        }

        public static ParticleSystem CreateDebris(Transform parent, Vector3 localPos, float sizeMul = 1f)
        {
            var ps = NewPs(parent, localPos, "Fx_Debris");
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = CubeMesh;
            r.material = MatAlpha;
            var m = ps.main;
            m.startLifetime = 0.9f;
            m.startSpeed = new ParticleSystem.MinMaxCurve(2f * sizeMul, 5f * sizeMul);
            m.startSize = new ParticleSystem.MinMaxCurve(0.06f * sizeMul, 0.12f * sizeMul);
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            m.startColor = new ParticleSystem.MinMaxGradient(new Color(0.32f, 0.32f, 0.35f), new Color(0.5f, 0.5f, 0.52f));
            m.gravityModifier = 1.8f;
            m.playOnAwake = false;
            m.loop = false;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f * sizeMul;
            var rot = ps.rotationOverLifetime; rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
            ps.Stop();
            return ps;
        }

        public static ParticleSystem CreateSmokeLoop(Transform parent, Vector3 localPos)
        {
            var ps = NewPs(parent, localPos, "Fx_Smoke");
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.material = MatAlpha;
            var m = ps.main;
            m.startLifetime = 1.6f;
            m.startSpeed = 0.7f;
            m.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            m.startColor = new Color(1f, 1f, 1f, 0.45f);
            m.loop = true;
            m.playOnAwake = false;
            var em = ps.emission; em.rateOverTime = 8f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 15f; shape.radius = 0.1f;
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(1f, 1.4f)));
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(new Gradient
            {
                alphaKeys = new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) },
                colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }
            });
            ps.Stop();
            return ps;
        }

        public static ParticleSystem CreateConfetti(Transform parent, Vector3 localPos)
        {
            var ps = NewPs(parent, localPos, "Fx_Confetti");
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = CubeMesh;
            r.material = MatAlpha;
            var m = ps.main;
            m.startLifetime = 2.8f;
            m.startSpeed = new ParticleSystem.MinMaxCurve(5f, 9f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            m.gravityModifier = 0.9f;
            m.playOnAwake = false;
            m.loop = false;
            var grad = new Gradient();
            grad.mode = GradientMode.Fixed;
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.2f, 0.2f), 0f),
                    new GradientColorKey(new Color(1f, 0.95f, 0.2f), 0.34f),
                    new GradientColorKey(new Color(0.2f, 1f, 0.3f), 0.67f),
                    new GradientColorKey(new Color(0.2f, 0.5f, 1f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            m.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
            var em = ps.emission; em.enabled = false;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 150) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 65f; shape.radius = 0.3f;
            var rot = ps.rotationOverLifetime; rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
            ps.Stop();
            return ps;
        }

        public static ParticleSystem CreateSparkle(Transform parent, Vector3 localPos)
        {
            var ps = NewPs(parent, localPos, "Fx_Sparkle");
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.material = MatAdditive;
            var m = ps.main;
            m.startLifetime = 0.6f;
            m.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            m.startColor = Color.white;
            m.playOnAwake = false; m.loop = false;
            var em = ps.emission; em.enabled = false;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.35f;
            ps.Stop();
            return ps;
        }

        /// <summary>One-shot blue ring + soft smoke at a world position. Auto-destroys.</summary>
        public static void SpawnPurchaseGlowAt(Vector3 worldPos)
        {
            var root = new GameObject("Fx_PurchaseGlow");
            root.transform.position = worldPos + new Vector3(0f, 0.05f, 0f);

            // Blue ring (additive, expanding).
            var ringGo = new GameObject("Ring");
            ringGo.transform.SetParent(root.transform, false);
            var ring = ringGo.AddComponent<ParticleSystem>();
            var rr = ring.GetComponent<ParticleSystemRenderer>(); rr.material = MatAdditive;
            var rm = ring.main;
            rm.startLifetime = 1.0f;
            rm.startSpeed = 0f;
            rm.startSize = 0.25f;
            rm.startColor = new Color(0.35f, 0.65f, 1f, 1f);
            rm.playOnAwake = false; rm.loop = false;
            var rem = ring.emission; rem.enabled = false;
            rem.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
            var rshape = ring.shape;
            rshape.shapeType = ParticleSystemShapeType.Circle;
            rshape.radius = 0.02f;
            rshape.arcMode = ParticleSystemShapeMultiModeValue.Loop;
            var rsol = ring.sizeOverLifetime; rsol.enabled = true;
            rsol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(1f, 3.5f)));
            var rcol = ring.colorOverLifetime; rcol.enabled = true;
            rcol.color = new ParticleSystem.MinMaxGradient(new Gradient
            {
                alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) },
                colorKeys = new[] { new GradientColorKey(new Color(0.35f, 0.65f, 1f), 0f), new GradientColorKey(new Color(0.35f, 0.65f, 1f), 1f) }
            });

            // Smoke puff (alpha).
            var smokeGo = new GameObject("Smoke");
            smokeGo.transform.SetParent(root.transform, false);
            var smoke = smokeGo.AddComponent<ParticleSystem>();
            var sr = smoke.GetComponent<ParticleSystemRenderer>(); sr.material = MatAlpha;
            var sm = smoke.main;
            sm.startLifetime = 1.2f;
            sm.startSpeed = 0.8f;
            sm.startSize = 0.6f;
            sm.startColor = new Color(1f, 1f, 1f, 0.6f);
            sm.playOnAwake = false; sm.loop = false;
            var sem = smoke.emission; sem.enabled = false;
            sem.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });
            var sshape = smoke.shape; sshape.shapeType = ParticleSystemShapeType.Cone; sshape.angle = 40f; sshape.radius = 0.2f;
            var ssol = smoke.sizeOverLifetime; ssol.enabled = true;
            ssol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 2f)));
            var scol = smoke.colorOverLifetime; scol.enabled = true;
            scol.color = new ParticleSystem.MinMaxGradient(new Gradient
            {
                alphaKeys = new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) },
                colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }
            });

            ring.Play(); smoke.Play();
            Object.Destroy(root, 2.5f);
        }

        public static void Burst(ParticleSystem ps, int count)
        {
            if (ps == null || count <= 0) return;
            ps.Emit(count);
        }
    }
}
