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
                
                default:
                    return false;
            }
        }

        private static ICharFXEffect CreateWave(CharFXTagData tag)
        {
            var amplitude = CharFXArgParser.Float(tag, 0, 8f);
            var frequency = CharFXArgParser.Float(tag, 1, 8f);
            var phaseOffset =  CharFXArgParser.Float(tag, 2, 0.5f);
            
            return new WaveEffect(amplitude, frequency, phaseOffset);
        }

        private static ICharFXEffect CreateShake(CharFXTagData tag)
        {
            var amplitude = CharFXArgParser.Float(tag, 0, 2f);
            var frequency = CharFXArgParser.Float(tag, 1, 40f);
            var seed = CharFXArgParser.UInt(tag, 2, 12345u);

            return new ShakeEffect(amplitude, frequency, seed);
        }

        private static ICharFXEffect CreatePop(CharFXTagData tag)
        {
            var duration = CharFXArgParser.Float(tag, 0, 0.35f);
            var scaleFrom = CharFXArgParser.Float(tag, 1, 0f);
            var stagger = CharFXArgParser.Float(tag, 2, 0.025f);
            
            return new PopEffect(duration, scaleFrom, stagger);
        }
    }
}