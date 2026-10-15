using UnityEngine;

namespace MyProject
{
    public class FXManager : MonoBehaviour
    {
        public static FXManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<FXManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("FXManager");
                        instance = go.AddComponent<FXManager>();
                    }
                }
                return instance;
            }
            private set => instance = value;
        }
        private static FXManager instance;

        [Header("Particle Systems")]
        private ParticleSystem jumpDustPS;
        private ParticleSystem coinSparklePS;
        private ParticleSystem debrisPS;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            InitializeParticleSystems();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void InitializeParticleSystems()
        {
            jumpDustPS = CreateParticleSystem("JumpDustFX", new Color(0.95f, 0.95f, 0.95f, 0.75f), 0.12f, 0.28f, false);
            coinSparklePS = CreateParticleSystem("CoinSparkleFX", new Color(1f, 0.88f, 0.25f, 0.95f), 0.15f, 0.45f, true);
            debrisPS = CreateParticleSystem("PlatformDebrisFX", new Color(0.92f, 0.65f, 0.38f, 0.9f), 0.14f, 0.5f, false);
        }

        private ParticleSystem CreateParticleSystem(string name, Color baseColor, float startSize, float lifetime, bool isSparkle)
        {
            Transform existing = transform.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<ParticleSystem>();
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = isSparkle ? 3.5f : 2.2f;
            main.startSize = startSize;
            main.startColor = baseColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            main.gravityModifier = isSparkle ? 0.3f : 1.2f;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(baseColor, 0f), new GradientColorKey(baseColor, 0.7f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(baseColor.a, 0f), new GradientAlphaKey(baseColor.a, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1.2f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                renderer.material = new Material(shader);
            }
            renderer.sortingOrder = 15;

            return ps;
        }

        public void EmitJumpDust(Vector3 position, Vector2 normal)
        {
            if (jumpDustPS == null) return;
            jumpDustPS.transform.position = position;

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true,
                startColor = new Color(0.92f, 0.92f, 0.95f, 0.75f)
            };

            int count = 6;
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(-45f, 45f) * Mathf.Deg2Rad;
                Vector2 dir = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg) * normal;
                emitParams.velocity = dir * Random.Range(1.2f, 2.5f);
                jumpDustPS.Emit(emitParams, 1);
            }
        }

        public void EmitCoinSparkle(Vector3 position)
        {
            if (coinSparklePS == null) return;
            coinSparklePS.transform.position = position;

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = position,
                startColor = new Color(1f, 0.9f, 0.3f, 1f)
            };

            int count = 12;
            for (int i = 0; i < count; i++)
            {
                float angle = (i * 360f / count + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                emitParams.velocity = dir * Random.Range(2.0f, 4.5f);
                coinSparklePS.Emit(emitParams, 1);
            }
        }

        public void EmitPlatformDebris(Vector3 position, Vector3 platformScale)
        {
            if (debrisPS == null) return;
            debrisPS.transform.position = position;

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                applyShapeToPosition = true,
                startColor = new Color(0.95f, 0.68f, 0.42f, 0.9f)
            };

            float spreadX = platformScale.x * 0.5f;
            int count = 10;
            for (int i = 0; i < count; i++)
            {
                Vector3 p = position + new Vector3(Random.Range(-spreadX, spreadX), Random.Range(-0.1f, 0.1f), 0f);
                emitParams.position = p;
                emitParams.velocity = new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(1.0f, 3.0f));
                debrisPS.Emit(emitParams, 1);
            }
        }
    }
}
