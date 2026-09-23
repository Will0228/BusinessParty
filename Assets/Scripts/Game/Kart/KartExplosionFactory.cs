using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace MixVerse.Game.Kart
{
    public sealed class KartExplosionFactory
    {
        private readonly KartStageView _stage;
        private readonly TMP_FontAsset _font;
        private readonly Material _glow;
        private readonly Material _smoke;
        private readonly Material _ring;
        private readonly Shader _fireShader;
        private readonly Mesh _debrisMesh;
        private readonly Material _debrisMaterial;
        private readonly Material _crackMaterial;
        private readonly float _radius;
        private readonly Color _gold = new Color(1f, 0.62f, 0.08f);

        public Material Glow => _glow;

        public KartExplosionFactory(KartStageView stage, TMP_FontAsset font, float radius)
        {
            _stage = stage;
            _font = font;
            _radius = radius;
            var shader = Resources.Load<Shader>("KartExplosionParticle");
            _glow = Material(shader, false, false);
            _smoke = Material(shader, false, true);
            _ring = Material(shader, true, false);
            _debrisMesh = DebrisMesh();
            _debrisMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _debrisMaterial.SetColor("_BaseColor", new Color(0.24f, 0.21f, 0.17f));
            _debrisMaterial.SetFloat("_Smoothness", 0f);
            _crackMaterial = new Material(Resources.Load<Shader>("KartGroundCracks"));
            _stage.GeneratedAssets.Add(_debrisMesh);
            _stage.GeneratedAssets.Add(_debrisMaterial);
            _stage.GeneratedAssets.Add(_crackMaterial);
            _fireShader = Shader.Find("MixVerse/ExplosionShaderURP");
        }

        public KartExplosionView Create(bool rocket, int seed, bool showCaption = true)
        {
            var root = new GameObject(rocket ? "Rocket fireworks" : "Crate impact");
            root.transform.SetParent(_stage.transform, false);
            var radius = _radius;
            var fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fireball.name = "White hot fireball";
            Object.Destroy(fireball.GetComponent<Collider>());
            fireball.transform.SetParent(root.transform, false);
            var fire = new Material(_fireShader);
            fireball.GetComponent<Renderer>().sharedMaterial = fire;
            fire.SetFloat("_Intensity", 3.2f);
            TextMeshPro caption = null;
            KartExplosionDebrisView debris = null;
            if (rocket)
            {
                var debrisRoot = new GameObject("Flying road debris");
                debrisRoot.transform.SetParent(root.transform, false);
                debris = debrisRoot.AddComponent<KartExplosionDebrisView>();
                debris.Initialize(_debrisMesh, _debrisMaterial, seed);
                Burst(root.transform, "Ignition flash", _glow, 1, 0f, 0.16f, 0.18f, 0f, 0f, 8f, 12f, Color.white, 0f, seed);
                var sparks = Burst(root.transform, "Outrageous radial sparks", _glow, 110, 0f, 0.55f, 1.25f, 12f, 27f, 0.1f, 0.23f, _gold, 0.6f, seed + 1);
                Stretch(sparks, 2.8f);
                var encore = Burst(root.transform, "Encore sparks", _glow, 65, 0.18f, 0.6f, 1.4f, 8f, 20f, 0.08f, 0.19f, _gold, 0.9f, seed + 2);
                Stretch(encore, 2f);
                Burst(root.transform, "Floating embers", _glow, 48, 0.06f, 1.3f, 2.5f, 3f, 10f, 0.09f, 0.2f, _gold, -0.08f, seed + 3);
                var smoke = Burst(root.transform, "Cartoon smoke puffs", _smoke, 16, 0.12f, 1.3f, 2.35f, 2f, 6f, 1.8f, 3.6f, new Color(0.22f, 0.25f, 0.3f, 0.55f), -0.12f, seed + 4);
                var smokeSize = smoke.sizeOverLifetime;
                smokeSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.6f));
                Shockwave(root.transform, "Ground shockwave", 0f, 24f, seed + 5);
                Shockwave(root.transform, "Second shockwave", 0.13f, 18f, seed + 6);
                if (showCaption) caption = Caption(root.transform);
            }
            var view = root.AddComponent<KartExplosionView>();
            view.Initialize(fireball.transform, fire, caption, _stage.Camera, radius, rocket, debris);
            return view;
        }

        private Mesh DebrisMesh()
        {
            var corners = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.38f), new Vector3(0.42f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.3f), new Vector3(-0.3f, -0.5f, 0.5f),
                new Vector3(-0.38f, 0.5f, -0.3f), new Vector3(0.32f, 0.35f, -0.35f),
                new Vector3(0.4f, 0.5f, 0.22f), new Vector3(-0.25f, 0.3f, 0.4f)
            };
            var faces = new[] { 0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6, 0, 4, 5, 0, 5, 1,
                1, 5, 6, 1, 6, 2, 2, 6, 7, 2, 7, 3, 3, 7, 4, 3, 4, 0 };
            var vertices = new Vector3[faces.Length];
            var indices = new int[faces.Length];
            for (var i = 0; i < faces.Length; i++) { vertices[i] = corners[faces[i]]; indices[i] = i; }
            var mesh = new Mesh { name = "Jagged asphalt slab", vertices = vertices, triangles = indices };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public void AddGroundCracks(KartExplosionView effect, float distance, float lane, float roadHalfWidth)
        {
            const int segments = 16;
            var radius = _radius * 1.35f;
            var vertices = new Vector3[(segments + 1) * (segments + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * segments * 6];
            var origin = _stage.Point(distance, lane) + Vector3.up * 0.8f;
            var inverse = Quaternion.Inverse(_stage.DirectionAt(distance));
            for (var z = 0; z <= segments; z++)
            for (var x = 0; x <= segments; x++)
            {
                var index = z * (segments + 1) + x;
                var lateral = Mathf.Clamp(lane + (x / (float)segments * 2f - 1f) * radius, -roadHalfWidth + 0.2f, roadHalfWidth - 0.2f);
                var along = (z / (float)segments * 2f - 1f) * radius;
                vertices[index] = inverse * (_stage.Point(distance + along, lateral) + Vector3.up * 0.065f - origin);
                uv[index] = new Vector2((lateral - lane) / (2f * radius) + 0.5f, z / (float)segments);
                if (x == segments || z == segments) continue;
                var t = (z * segments + x) * 6;
                triangles[t] = index; triangles[t + 1] = index + segments + 1; triangles[t + 2] = index + 1;
                triangles[t + 3] = index + 1; triangles[t + 4] = index + segments + 1; triangles[t + 5] = index + segments + 2;
            }
            var mesh = new Mesh { name = "Road conforming blast cracks", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateBounds();
            var obj = new GameObject("Blast cracks and scorch", typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(effect.transform, false);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _crackMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            effect.SetGroundCracks(renderer, mesh);
        }

        private Material Material(Shader shader, bool ring, bool smoke)
        {
            var material = new Material(shader);
            material.SetFloat("_Ring", ring ? 1f : 0f);
            material.SetFloat("_DstBlend", (float)(smoke ? BlendMode.OneMinusSrcAlpha : BlendMode.One));
            _stage.GeneratedAssets.Add(material);
            return material;
        }

        private ParticleSystem Burst(Transform parent, string name, Material material, int count, float delay,
            float minLife, float maxLife, float minSpeed, float maxSpeed, float minSize, float maxSize,
            Color color, float gravity, int seed)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var particles = obj.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.useAutoRandomSeed = false;
            particles.randomSeed = (uint)Mathf.Max(1, seed);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.1f;
            main.startDelay = delay;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = count;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.25f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 0.15f), new GradientColorKey(new Color(1f, 0.35f, 0.08f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.45f), new GradientAlphaKey(0f, 1f) });
            colors.color = gradient;
            if (material == _smoke)
            {
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.65f, 0.2f), new GradientAlphaKey(0f, 1f) });
                colors.color = gradient;
            }
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.maxParticleSize = 2f;
            particles.Play();
            return particles;
        }

        private void Stretch(ParticleSystem particles, float length)
        {
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = length;
            renderer.velocityScale = 0.12f;
        }

        private void Shockwave(Transform root, string name, float delay, float diameter, int seed)
        {
            var particles = Burst(root, name, _ring, 1, delay, 0.5f, 0.5f, 0f, 0f, diameter, diameter, _gold, 0f, seed);
            particles.transform.localPosition = Vector3.down * 0.65f;
            var shape = particles.shape;
            shape.enabled = false;
            var size = particles.sizeOverLifetime;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.08f, 1f, 1f));
            particles.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        }

        private TextMeshPro Caption(Transform parent)
        {
            var obj = new GameObject("Comically excessive caption", typeof(TextMeshPro));
            obj.transform.SetParent(parent, false);
            var caption = obj.GetComponent<TextMeshPro>();
            caption.font = _font;
            caption.text = "ドカーン!!";
            caption.fontSize = 28f;
            caption.fontStyle = FontStyles.Bold | FontStyles.Italic;
            caption.color = new Color(1f, 0.9f, 0.18f);
            caption.alignment = TextAlignmentOptions.Center;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.rectTransform.sizeDelta = new Vector2(24f, 5f);
            caption.outlineColor = new Color(0.2f, 0.035f, 0.015f);
            caption.outlineWidth = 0.25f;
            return caption;
        }
    }
}
