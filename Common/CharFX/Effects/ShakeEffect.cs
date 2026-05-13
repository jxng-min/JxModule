using UnityEngine;

namespace JxModule.CharFX
{
    public class ShakeEffect : ICharFXEffect
    {
        private readonly float _amplitude;
        private readonly float _frequency;
        private readonly uint _seed;

        public ShakeEffect(float amplitude, float frequency, uint seed)
        {
            _amplitude = amplitude;
            _frequency = frequency;
            _seed = seed;
        }
        
        public void Apply(int charIndex, ref CharQuad quad, in CharFXContext context)
        {
            var baseSeed = (uint)charIndex * 747796405u ^ _seed;

            var x = HashUtility.Hash11Signed(baseSeed);
            var y = HashUtility.Hash11Signed(baseSeed ^ 0x9E377878u);

            var power = Mathf.Sin(context.Time * _frequency * charIndex * 0.37f);
            var offset = _amplitude * power * new Vector3(x, y, 0f);
            
            quad.Translate(offset);
        }
    }
}