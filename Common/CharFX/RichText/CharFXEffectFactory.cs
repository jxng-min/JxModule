namespace JxModule.CharFX
{
    public static class CharFXEffectFactory
    {
        public static bool TryCreateEffect(in CharFXTagData tag, out ICharFXEffect effect)
        {
            effect = null;

            var name = tag.Name?.ToLowerInvariant();
            switch (name)
            {
                case "wave":
                    effect = CreateWave(tag);
                    return true;
                
                case "shake":
                    effect = CreateShake(tag);
                    return true;
                
                case "pop":
                    effect = CreatePop(tag);
                    return true;
                
                case "bounce":
                    effect = CreateBounce(tag);
                    return true;
                
                case "wiggle":
                    effect = CreateWiggle(tag);
                    return true;
                
                case "pulse":
                    effect = CreatePulse(tag);
                    return true;
                
                default:
                    return false;
            }
        }

        private static ICharFXEffect CreateWave(CharFXTagData tag)
        {
            var amplitude = CharFXArgParser.Float(tag, 0, 5f);
            var frequency = CharFXArgParser.Float(tag, 1, 3f);
            var phaseOffset =  CharFXArgParser.Float(tag, 2, 1f);
            
            return new WaveEffect(amplitude, frequency, phaseOffset);
        }

        private static ICharFXEffect CreateShake(CharFXTagData tag)
        {
            var intensity = CharFXArgParser.Float(tag, 0, 5f);
            var frequency = CharFXArgParser.Float(tag, 1, 10f);
            var charOffset = CharFXArgParser.Float(tag, 2, 0.5f);

            return new ShakeEffect(intensity, frequency, charOffset);
        }

        private static ICharFXEffect CreatePop(CharFXTagData tag)
        {
            var duration = CharFXArgParser.Float(tag, 0, 1.5f);
            var scaleFrom = CharFXArgParser.Float(tag, 1, 0.75f);
            var stagger = CharFXArgParser.Float(tag, 2, 0.1f);
            
            return new PopEffect(duration, scaleFrom, stagger);
        }

        private static ICharFXEffect CreateBounce(CharFXTagData tag)
        {
            var height = CharFXArgParser.Float(tag, 0, 20f);
            var speed = CharFXArgParser.Float(tag, 1, 7f);
            var charOffset = CharFXArgParser.Float(tag, 2, 0f);
            
            return new BounceEffect(height, speed, charOffset);
        }

        private static ICharFXEffect CreateWiggle(CharFXTagData tag)
        {
            var angle = CharFXArgParser.Float(tag, 0, 20f);
            var speed = CharFXArgParser.Float(tag, 1, 3f);
            var charOffset = CharFXArgParser.Float(tag, 2, 0.25f);
            
            return new WiggleEffect(angle, speed, charOffset);
        }

        private static ICharFXEffect CreatePulse(CharFXTagData tag)
        {
            var scale = CharFXArgParser.Float(tag, 0, 0.15f);
            var speed = CharFXArgParser.Float(tag, 1, 4f);
            var charOffset = CharFXArgParser.Float(tag, 2, 0.25f);
            
            return new PulseEffect(scale, speed, charOffset);
        }
    }
}