using UnityEngine;

namespace GolpeCalculado
{
    // Garrafa lançada com G: no primeiro impacto faz ruído e atrai o vigilante.
    public sealed class ThrownNoise : MonoBehaviour
    {
        public HeistGame game;
        [Tooltip("Alcance do ruído do impacto.")] public float noiseRange = 18;

        bool sounded;

        void OnCollisionEnter(Collision collision)
        {
            if (sounded)
                return;
            sounded = true;

            game.Noise(transform.position, noiseRange);
            game.Tone(900);
            game.Discover("ruido", "Os impactos atraem o vigilante ao local do som; a ronda fica temporariamente livre.");
            Destroy(gameObject, 8);
        }
    }
}
